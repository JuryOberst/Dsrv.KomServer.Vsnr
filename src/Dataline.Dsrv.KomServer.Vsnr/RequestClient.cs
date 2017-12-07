// <copyright file="RequestClient.cs" company="DATALINE GmbH &amp; Co. KG">
// Copyright (c) DATALINE GmbH &amp; Co. KG. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

using BeanIO;

using Dataline.Entities;

using ExtraStandard;
using ExtraStandard.DrvKomServer.Extra14;
using ExtraStandard.DrvKomServer.Extra14.Dsv;
using ExtraStandard.Extra14;
using ExtraStandard.Validation;

using NodaTime;

using SocialInsurance.Germany.Messages.Mappings;
using SocialInsurance.Germany.Messages.Pocos;

using ExtraDataType = ExtraStandard.DrvKomServer.Extra14.Dsv.ExtraDataType;

namespace Dsrv.KomServer.Vsnr
{
    /// <summary>
    /// Client für die Kommunikation mit dem DSRV-Kommunikationsserver
    /// </summary>
    public class RequestClient : IDisposable
    {
        private static readonly Lazy<StreamFactory> _streamFactory = new Lazy<StreamFactory>(() =>
        {
            var streamFactory = StreamFactory.NewInstance();
            using (var meldungen = Meldungen.LoadMeldungen())
            {
                streamFactory.Load(meldungen);
            }

            return streamFactory;
        });

        private readonly AbsenderOnlineversand _sender;

        private readonly Firma _company;

        private readonly bool _isTest;

        private readonly IDrvDsvExtra14ValidatorFactory _validatorFactory;

        private readonly IExtraEncryptionHandler _encryptionHandler;

        private readonly IExtraCompressionHandler _compressionHandler = new ExtraStandard.Compression.DsrvZipGzipCompressionHandler();

        private readonly IRequestMessageValidator _messageValidator;

        private readonly Lazy<HttpClient> _httpClient;

        private bool _disposedValue; // Dient zur Erkennung redundanter Aufrufe.

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="RequestClient"/> Klasse.
        /// </summary>
        /// <param name="sender">Informationen über den Absender</param>
        /// <param name="company">Informationen über die Firma, die die Daten abfragt</param>
        /// <param name="isTest">Soll das Test-System verwendet werden?</param>
        /// <param name="encryptionHandler">Das zu verwendende Verschlüsselungsverfahren</param>
        /// <param name="validatorFactory">Der zu verwendende eXTra-Verfahren-Validator</param>
        /// <param name="messageValidator">Der zu verwendende Prüfer für die DSVV-Meldungen</param>
        /// <param name="httpClientFactory">Eine Factory über die ein <see cref="HttpClient"/> erstellt werden kann</param>
        public RequestClient(AbsenderOnlineversand sender, Firma company, bool isTest, IExtraEncryptionHandler encryptionHandler, IDrvDsvExtra14ValidatorFactory validatorFactory, IRequestMessageValidator messageValidator, IHttpClientFactory httpClientFactory)
        {
            _encryptionHandler = encryptionHandler;
            _validatorFactory = validatorFactory;
            _messageValidator = messageValidator;
            _isTest = isTest;
            _company = company;
            _sender = sender;
            _httpClient = new Lazy<HttpClient>(httpClientFactory.CreateHttpClient);
        }

        internal StreamFactory StreamFactory => _streamFactory.Value;

        /// <summary>
        /// Freigabe der Ressourcen nach dem Dispose-Muster
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Führt einen Versand der Versicherungsnummernabfrage-Daten aus
        /// </summary>
        /// <param name="ct">Ein <see cref="CancellationToken"/> über das die Abfrage abgebrochen werden kann</param>
        /// <param name="fileNumber">Die Dateinummer die für die Abfrage zu verwenden ist</param>
        /// <param name="persons">Die Mitarbeiter die abgefragt werden</param>
        /// <returns>Die eXTra-Rückmeldung der Anfrage</returns>
        public async Task<TransportResponseType> ExecuteSend(CancellationToken ct, int fileNumber, IEnumerable<Person> persons)
        {
            var requestTimestamp = DateTime.Now;
            var requestFile = BuildRequestFile(_streamFactory.Value, fileNumber, requestTimestamp, persons);
            var requestId = Guid.NewGuid().ToString("N");

            var request = CreateDelivery(requestId, requestTimestamp, fileNumber, requestFile);
            var responseData = await ExecuteRequest(ct, request).ConfigureAwait(false);

            var response = GetResponse(responseData);
            return response;
        }

        /// <summary>
        /// Fragt die Ergebnisse der Versicherungsnummernabfrage ab
        /// </summary>
        /// <param name="ct">Ein <see cref="CancellationToken"/> über das die Abfrage abgebrochen werden kann</param>
        /// <param name="lastResponseId">Die ID der Antwort von <see cref="ExecuteSend(CancellationToken, int, IEnumerable{Person})"/> minus 1, da wir hier nur auf &gt; <paramref name="lastResponseId"/> abfragen können</param>
        /// <returns>Die eXTra-Rückmeldung der Anfrage</returns>
        public async Task<TransportResponseType> ExecuteQuery(CancellationToken ct, string lastResponseId)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var requestTimestamp = DateTime.Now;
            var request = CreateQuery(requestId, requestTimestamp, lastResponseId);
            var responseData = await ExecuteRequest(ct, request).ConfigureAwait(false);
            var response = GetResponse(responseData);
            return response;
        }

        /// <summary>
        /// Quittiert die Ergebnisse der Versicherungsnummernabfrage
        /// </summary>
        /// <param name="ct">Ein <see cref="CancellationToken"/> über das die Abfrage abgebrochen werden kann</param>
        /// <param name="responseIds">Die IDs der Antworten von <see cref="ExecuteQuery(CancellationToken, string)"/></param>
        /// <returns>Die eXTra-Rückmeldung der Quittierung</returns>
        public async Task<TransportResponseType> ExecuteAcknowledge(CancellationToken ct, params string[] responseIds)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var requestTimestamp = DateTime.Now;
            var request = CreateAcknowledge(requestId, requestTimestamp, responseIds);
            var responseData = await ExecuteRequest(ct, request).ConfigureAwait(false);
            var response = GetResponse(responseData);
            return response;
        }

        /// <summary>
        /// Entschlüsselung der Paket-Daten, die von <see cref="ExecuteQuery(CancellationToken, string)"/> zurückgeliefert werden
        /// </summary>
        /// <param name="response">Die Rückmeldung von <see cref="ExecuteQuery(CancellationToken, string)"/></param>
        /// <returns>Die in <paramref name="response"/> gefundenen eXTra-Pakete</returns>
        public IReadOnlyCollection<PackageInfo> DecryptPackages(TransportResponseType response)
        {
            return DecodePackages(response.TransportBody?.Items?.Cast<PackageResponseType>());
        }

        /// <summary>
        /// Freigabe der Ressourcen nach dem Dispose-Muster
        /// </summary>
        /// <param name="disposing">Wirklich freigeben?</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_httpClient.IsValueCreated)
                    {
                        _httpClient.Value.Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        private static TransportResponseType GetResponse(byte[] responseData)
        {
            var responseDoc = XDocument.Load(new MemoryStream(responseData));
            if (responseDoc.Root == null)
            {
                throw new VsnrException(Encoding.UTF8.GetString(responseData, 0, responseData.Length));
            }

            if (responseDoc.Root.Name.LocalName == "XMLError")
            {
                var serializer = ExtraStandard.ExtraUtilities.GetSerializer<XmlErrorType>();
                var error = (XmlErrorType)serializer.Deserialize(responseDoc.CreateReader());
                throw new ExtraMessageException(error.Flag.Select(x => x.AsExtraFlag()));
            }

            var responseSerializer = ExtraStandard.ExtraUtilities.GetSerializer<TransportResponseType>();
            var response = (TransportResponseType)responseSerializer.Deserialize(responseDoc.CreateReader());

            if (response.TransportHeader.ResponseDetails.Report.highestWeight == ExtraFlagWeight.Error)
            {
                throw new ExtraMessageException(response.TransportHeader.ResponseDetails.Report.Flag.Select(x => x.AsExtraFlag()));
            }

            return response;
        }

        private static byte[] XmlToBytes(XDocument document, Encoding encoding)
        {
            using (var output = new MemoryStream())
            {
                var writerSettings = new XmlWriterSettings()
                {
                    Encoding = encoding,
                    CloseOutput = true,
                    WriteEndDocumentOnClose = true,
                    Indent = false,
                };

                using (var writer = XmlWriter.Create(output, writerSettings))
                {
                    document.Save(writer);
                }

                return output.ToArray();
            }
        }

        private async Task<byte[]> ExecuteRequest(CancellationToken ct, XDocument request)
        {
            var encoding = Encoding.GetEncoding("iso-8859-1");
            var requestData = XmlToBytes(request, encoding);

            var client = _httpClient.Value;
            var content = new ByteArrayContent(requestData)
            {
                Headers =
                {
                    ContentType = MediaTypeHeaderValue.Parse($"text/xml; charset={encoding.WebName}"),
                },
            };

            var requestUrl = _isTest ? DsrvConstants.RequestUrlTest : DsrvConstants.RequestUrlProd;
            var responseMessage = await client.PostAsync(requestUrl, content, ct).ConfigureAwait(false);
            await responseMessage.Content.LoadIntoBufferAsync().ConfigureAwait(false);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new VsnrException($"{responseMessage.StatusCode}: {responseMessage.ReasonPhrase}")
                {
                    Data =
                    {
                        { "content", await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) },
                    },
                };
            }

            var responseData = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            return responseData;
        }

        private void Validate(string[] records)
        {
            if (_messageValidator == null)
            {
                return;
            }

            var voszRecord = records[0];
            var dsvvRecords = records.Skip(2).Take(records.Length - 3).ToList();
            var errorMessages = _messageValidator.Validate(voszRecord, dsvvRecords);
            if (errorMessages.Count != 0)
            {
                throw new ValidationException(errorMessages);
            }
        }

        private XDocument CreateDelivery(string requestId, DateTime requestTimestamp, int fileNumber, string data)
        {
            var encodingId = "I1";
            var encoding = ExtraEncodingFactory.Dsrv.GetEncoding(encodingId);
            var requestData = encoding.GetBytes(data);

            var dataName = $"{(_isTest ? 'T' : 'E')}DSV0{fileNumber:D6}";
            var dataTransformsHelper = new ExtraDataTransformHandler(new[] { _compressionHandler }, new[] { _encryptionHandler });
            var transformResult = dataTransformsHelper.Transform(requestData, dataName, requestTimestamp, _compressionHandler.AlgorithmId, _encryptionHandler.AlgorithmId);
            var requestDataEncrypted = transformResult.Item1;

            var extra = new TransportRequestType()
            {
                version = SupportedVersionsType.Item14,
                profile = "http://www.extra-standard.de/profile/DEUEV/2.0",
                TransportHeader = new TransportRequestHeaderType()
                {
                    Sender = new SenderType()
                    {
                        SenderID = new ClassifiableIDType() { Value = _sender.Betriebsnummer },
                    },
                    Receiver = new ReceiverType()
                    {
                        ReceiverID = new ClassifiableIDType() { Value = DsrvConstants.Betriebsnummer },
                    },
                    RequestDetails = new RequestDetailsType()
                    {
                        RequestID = new ClassifiableIDType()
                        {
                            Value = requestId,
                        },
                        TimeStamp = requestTimestamp.ToUniversalTime(),
                        TimeStampSpecified = true,
                        Application = new ApplicationType()
                        {
                            Manufacturer = "DATALINE GmbH & Co. KG",
                            Product = new TextType() { Value = "DATALINE Lohnabzug" },
                        },
                        Procedure = DsrvConstants.ProcedureSend,
                        DataType = ExtraDataType.VSNRAnfrage,
                        Scenario = ExtraScenario.RequestWithAcknowledgement,
                    },
                },
                TransportPlugIns = new AnyPlugInContainerType()
                {
                    Any = new object[]
                    {
                        new DataTransformsType()
                        {
                            version = DataTransformsTypeVersion.Item12,
                            versionSpecified = true,
                            Compression = transformResult.Item2.OfType<CompressionType>().ToArray(),
                            Encryption = transformResult.Item2.OfType<EncryptionType>().ToArray(),
                        },
                        new DataSourceType()
                        {
                            version = DataSourceTypeVersion.Item10,
                            versionSpecified = true,
                            DataContainer = new DataContainerType()
                            {
                                type = ExtraContainerType.File,
                                created = requestTimestamp.ToUniversalTime(),
                                createdSpecified = true,
                                encoding = encodingId,
                                name = dataName,
                            },
                        },
                        new ContactsType()
                        {
                            version = ContactsTypeVersion.Item10,
                            versionSpecified = true,
                            SenderContact = new[]
                            {
                                new ContactType()
                                {
                                    Endpoint = new[]
                                    {
                                        new EndpointType()
                                        {
                                            type = EndpointTypeType.SMTP,
                                            Value = _sender.Email,
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                TransportBody = new TransportRequestBodyType()
                {
                    Items = new object[]
                    {
                        new DataType1()
                        {
                            Item = new Base64CharSequenceType()
                            {
                                Value = requestDataEncrypted,
                            },
                        },
                    },
                },
            };

            var ns = new XmlSerializerNamespaces();
            ns.Add("xreq", "http://www.extra-standard.de/namespace/request/1");
            ns.Add("xcpt", "http://www.extra-standard.de/namespace/components/1");
            ns.Add("xplg", "http://www.extra-standard.de/namespace/plugins/1");

            var serializer = ExtraStandard.ExtraUtilities.GetSerializer<TransportRequestType>();
            var output = new MemoryStream();
            using (var writer = new StreamWriter(output))
            {
                serializer.Serialize(writer, extra, ns);
            }

            var serialized = output.ToArray();
            var document = XDocument.Load(new MemoryStream(serialized));

            var validator = _validatorFactory.Create(ExtraMessageType.SupplyData, ExtraTransportDirection.Request, false);
            validator.Validate(document);

            return document;
        }

        private XDocument CreateQuery(string requestId, DateTime requestTimestamp, string acceptResponseId)
        {
            var extra = new TransportRequestType()
            {
                version = SupportedVersionsType.Item14,
                profile = "http://www.extra-standard.de/profile/DEUEV/2.0",
                TransportHeader = new TransportRequestHeaderType()
                {
                    Sender = new SenderType()
                    {
                        SenderID = new ClassifiableIDType() { Value = _sender.Betriebsnummer },
                    },
                    Receiver = new ReceiverType()
                    {
                        ReceiverID = new ClassifiableIDType() { Value = DsrvConstants.Betriebsnummer },
                    },
                    RequestDetails = new RequestDetailsType()
                    {
                        RequestID = new ClassifiableIDType()
                        {
                            Value = requestId,
                        },
                        TimeStamp = requestTimestamp.ToUniversalTime(),
                        TimeStampSpecified = true,
                        Application = new ApplicationType()
                        {
                            Manufacturer = "DATALINE GmbH & Co. KG",
                            Product = new TextType() { Value = "DATALINE Lohnabzug" },
                        },
                        Procedure = DsrvConstants.ProcedureQuery,
                        DataType = ExtraStandard.ExtraDataType.DataRequest,
                        Scenario = ExtraScenario.RequestWithResponse,
                    },
                },
                TransportBody = new TransportRequestBodyType()
                {
                    Items = new object[]
                    {
                        new DataType1()
                        {
                            Item = new ElementSequenceType()
                            {
                                Any = new object[]
                                {
                                    new DataRequestType()
                                    {
                                        Query = new[]
                                        {
                                            new DataRequestArgumentType()
                                            {
                                                property = ExtraStatusRequestPropertyName.ResponseID,
                                                type = XSDPrefixedTypeCodes1.xsstring,
                                                @event = EventNamesType.httpwwwextrastandarddeeventSendData,
                                                Items = new object[]
                                                {
                                                    new OperandType() { Value = acceptResponseId },
                                                },
                                                ItemsElementName = new[]
                                                {
                                                    ItemsChoiceType4.GT,
                                                },
                                            },
                                            new DataRequestArgumentType()
                                            {
                                                property = ExtraStatusRequestPropertyName.Procedure,
                                                Items = new object[]
                                                {
                                                    new OperandType() { Value = "DSV" },
                                                },
                                                ItemsElementName = new[]
                                                {
                                                    ItemsChoiceType4.EQ,
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            var ns = new XmlSerializerNamespaces();
            ns.Add("xreq", "http://www.extra-standard.de/namespace/request/1");
            ns.Add("xcpt", "http://www.extra-standard.de/namespace/components/1");
            ns.Add("xplg", "http://www.extra-standard.de/namespace/plugins/1");
            ns.Add("xmsg", "http://www.extra-standard.de/namespace/message/1");

            var serializer = ExtraStandard.ExtraUtilities.GetSerializer<TransportRequestType>();
            var output = new MemoryStream();
            using (var writer = new StreamWriter(output) { AutoFlush = true })
            {
                serializer.Serialize(writer, extra, ns);
            }

            var serialized = output.ToArray();

            var document = XDocument.Load(new MemoryStream(serialized));

            var validator = _validatorFactory.Create(ExtraMessageType.GetProcessingResult, ExtraTransportDirection.Request, false);
            validator.Validate(document);

            return document;
        }

        private XDocument CreateAcknowledge(string requestId, DateTime requestTimestamp, params string[] responseIds)
        {
            var extra = new TransportRequestType()
            {
                version = SupportedVersionsType.Item14,
                profile = "http://www.extra-standard.de/profile/DEUEV/2.0",
                TransportHeader = new TransportRequestHeaderType()
                {
                    Sender = new SenderType()
                    {
                        SenderID = new ClassifiableIDType() { Value = _sender.Betriebsnummer },
                    },
                    Receiver = new ReceiverType()
                    {
                        ReceiverID = new ClassifiableIDType() { Value = DsrvConstants.Betriebsnummer },
                    },
                    RequestDetails = new RequestDetailsType()
                    {
                        RequestID = new ClassifiableIDType()
                        {
                            Value = requestId,
                        },
                        TimeStamp = requestTimestamp.ToUniversalTime(),
                        TimeStampSpecified = true,
                        Application = new ApplicationType()
                        {
                            Manufacturer = "DATALINE GmbH & Co. KG",
                            Product = new TextType() { Value = "DATALINE Lohnabzug" },
                        },
                        Procedure = DsrvConstants.ProcedureAcknowledge,
                        DataType = ExtraStandard.ExtraDataType.ConfirmationOfReceipt,
                        Scenario = ExtraScenario.RequestWithAcknowledgement,
                    },
                },
                TransportBody = new TransportRequestBodyType()
                {
                    Items = new object[]
                    {
                        new DataType1()
                        {
                            Item = new ElementSequenceType()
                            {
                                Any = new object[]
                                {
                                    new ConfirmationOfReceiptType()
                                    {
                                        version = ConfirmationOfReceiptTypeVersion.Item13,
                                        PropertySet = new PropertySetType()
                                        {
                                            name = ExtraPropertySetName.ResponseID,
                                            Value = responseIds
                                                .Select(x => new ExtraStandard.Extra14.ValueType()
                                                {
                                                    Value = x,
                                                }).ToArray(),
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            var ns = new XmlSerializerNamespaces();
            ns.Add("xreq", "http://www.extra-standard.de/namespace/request/1");
            ns.Add("xcpt", "http://www.extra-standard.de/namespace/components/1");
            ns.Add("xplg", "http://www.extra-standard.de/namespace/plugins/1");
            ns.Add("xmsg", "http://www.extra-standard.de/namespace/message/1");

            var serializer = ExtraStandard.ExtraUtilities.GetSerializer<TransportRequestType>();
            var output = new MemoryStream();
            using (var writer = new StreamWriter(output) { AutoFlush = true })
            {
                serializer.Serialize(writer, extra, ns);
            }

            var serialized = output.ToArray();

            var document = XDocument.Load(new MemoryStream(serialized));

            var validator = _validatorFactory.Create(ExtraMessageType.AcknowledgeProcessingResult, ExtraTransportDirection.Request, false);
            validator.Validate(document);

            return document;
        }

        private string BuildRequestFile(StreamFactory streamFactory, int fileNumber, DateTime creationTimestamp, IEnumerable<Person> personen)
        {
            var registration = Dataline.Common.Constants.SocialInsurance.Registrations.Dataline;
            var output = new StringWriter();
            using (var writer = streamFactory.CreateWriter("dsvv-deuev-v01", output))
            {
                var vosz = new VOSZ()
                {
                    VFMM = "AGTRV",
                    ABSN = _sender.Betriebsnummer,
                    EPNR = DsrvConstants.Betriebsnummer,
                    ED = LocalDateTime.FromDateTime(creationTimestamp).Date,
                    DTNR = fileNumber,
                    NAAB = _sender.Name1,
                };
                writer.Write(vosz);

                var dsko = new DSKO04()
                {
                    VF = "DEUEV",
                    ABSN = _sender.Betriebsnummer,
                    EPNR = DsrvConstants.Betriebsnummer,
                    ED = creationTimestamp,
                    ABSNER = _company.Betriebsnummer,
                    PRODID = registration.ProductId,
                    MODID = registration.ModificationId,
                    NAME1 = _company.Name1,
                    PLZ = _company.PLZ,
                    ORT = _company.Ort,
                    STR = _company.Strasse,
                    ANRAP = _sender.IstFrau ? "W" : "M",
                    NAMEAP = _sender.Ansprechpartner,
                    TELAP = _sender.Telefon,
                    FAXAP = _sender.Fax,
                    EMAILAP = _sender.Email,
                };
                writer.Write(dsko);

                var personCount = 0;
                foreach (var person in personen)
                {
                    personCount += 1;

                    var dsvv = new DSVV01()
                    {
                        ABSN = _sender.Betriebsnummer,
                        EPNR = DsrvConstants.Betriebsnummer,
                        ED = creationTimestamp,
                        KENNZRM = DSVV01Status.Grundstellung,
                        BBNRVU = _company.Betriebsnummer,
                        DSID = XmlConvert.ToString(person.Id),
                        AZVU = person.Personalnummer,
                        MMUEB = 1,
                        DBNA = new DBNA()
                        {
                            FMNA = person.Nachname,
                            VONA = person.Vorname,
                        },
                        DBGB = new DBGB()
                        {
                            GBDT = LocalDate.FromDateTime(person.Gebdat),
                            GBNA = person.GeburtsName,
                            GBOT = person.GeburtsOrt,
                            GE = person.IstMann ? "M" : "W",
                        },
                        DBAN = new DBAN()
                        {
                            PLZ = person.PLZ,
                            ORT = person.Ort,
                        },
                    };
                    writer.Write(dsvv);
                }

                var ncsz = new NCSZ()
                {
                    VFMM = "AGTRV",
                    ABSN = _sender.Betriebsnummer,
                    EPNR = DsrvConstants.Betriebsnummer,
                    ED = LocalDateTime.FromDateTime(creationTimestamp).Date,
                    DTNR = fileNumber,
                    ZLSZ = personCount + 1,
                };
                writer.Write(ncsz);

                writer.Close();
            }

            var records = output.ToString().Split('\n').Select(x => x.TrimEnd('\r')).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            Validate(records);

            return string.Join("\r\n", records);
        }

        private IReadOnlyCollection<PackageInfo> DecodePackages(IEnumerable<PackageResponseType> packageResponses)
        {
            if (packageResponses == null)
            {
                return null;
            }

            var dataTransformsHelper = new ExtraDataTransformHandler(new[] { _compressionHandler }, new[] { _encryptionHandler });

            return packageResponses.Select(x => new PackageInfo(x, dataTransformsHelper, this)).ToList();
        }
    }
}
