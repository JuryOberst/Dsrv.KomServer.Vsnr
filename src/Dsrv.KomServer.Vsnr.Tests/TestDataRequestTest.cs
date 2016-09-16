using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Autofac;

using Dataline.Entities;

using ExtraStandard.Extra14;

using SocialInsurance.Germany.Messages.Pocos;

using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace Dsrv.KomServer.Vsnr.Tests
{
    public class TestDataRequestTest : IClassFixture<RequestClientInit>
    {
        private readonly IContainer _container;

        private readonly ITestOutputHelper _testOutput;

        public TestDataRequestTest(ITestOutputHelper testOutput, RequestClientInit clientInit)
        {
            _container = clientInit.Container;
            _testOutput = testOutput;
        }

        [Fact]
        public async Task TestTestPersons()
        {
            var maxResponseTime = TimeSpan.FromMinutes(3);

            var persons = TestData.TestPersons;
            var client = _container.Resolve<RequestClient>();
            var deliveryResponse = await client.ExecuteSend(CancellationToken.None, 1, persons);
            var responseId = XmlConvert.ToString(XmlConvert.ToInt64(deliveryResponse.TransportHeader.ResponseDetails.ResponseID.Value) - 1);
            _testOutput.WriteLine($"Query for responses > {responseId}");

            var startTime = DateTimeOffset.UtcNow;
            var responseIdsToAcknowledge = new List<string>();
            for (;;)
            {
                var queryResponse = client.ExecuteQuery(CancellationToken.None, responseId).Result;
                foreach (var flag in queryResponse.TransportHeader.GetFlags())
                {
                    _testOutput.WriteLine($"{flag.Code.Value}: {flag.Text.Value}");
                }

                if (queryResponse.TransportHeader.GetFlags().Any(x => x.Code.Value != "E97"))
                {
                    if (queryResponse.TransportHeader.GetFlags().All(x => x.Code.Value == "E98"))
                    {
                        if (queryResponse.TransportBody?.Items != null)
                        {
                            var timestamp = DateTime.Now;
                            var packageInfos = client.DecryptPackages(queryResponse);
                            foreach (var packageInfo in packageInfos)
                            {
                                _testOutput.WriteLine($"Datensendung {packageInfo.ResponseId}");

                                foreach (var flag in packageInfo.Flags)
                                {
                                    _testOutput.WriteLine($"{flag.Code.Value}: {flag.Text.Value}");
                                }

                                if (!packageInfo.IsError)
                                {
                                    File.WriteAllBytes($@"D:\temp\test-{timestamp:yyyyMMdd-HHmmssfff}-rx-query-data.txt", packageInfo.FileData);

                                    var recordIdToPerson = persons.ToDictionary(x => x.Id);

                                    var fileData = packageInfo.Decode();
                                    foreach (var datensatz in fileData.Datensaetze)
                                    {
                                        var hatFehler = false;
                                        foreach (var dbfe in datensatz.DBFE.Select(x => new ValidationError(x)).Where(x => x.IsError))
                                        {
                                            _testOutput.WriteLine($"Fehler {dbfe.Code}: {dbfe.Message}");
                                            hatFehler = true;
                                        }

                                        if (!hatFehler && datensatz.KE == "DSVV")
                                        {
                                            var responseMessage = new StringBuilder();

                                            var dsvv = (DSVV01)datensatz;

                                            responseMessage.Append($"Mitarbeiter {dsvv.DBNA.FMNA}, {dsvv.DBNA.VONA}");

                                            Person personRequest;
                                            if (recordIdToPerson.TryGetValue(XmlConvert.ToInt32(dsvv.DSID), out personRequest))
                                            {
                                                responseMessage.Append($" ({personRequest.Id}) aus {personRequest.PLZ} {personRequest.Ort}");
                                            }
                                            else
                                            {
                                                responseMessage.Append(" (fehlgeschlagene Zuordnung)");
                                            }


                                            switch (dsvv.KENNZRM)
                                            {
                                                case DSVV01Status.KeinErgebnis:
                                                    responseMessage.Append(" hat keine VSNR");
                                                    break;
                                                case DSVV01Status.EindeutigesErgebnis:
                                                    responseMessage.Append($" hat die VSNR {dsvv.VSNR}");
                                                    break;
                                                case DSVV01Status.KeinEindeutigesErgebnis:
                                                    responseMessage.Append(" konnte nicht eindeutig zugeordnet werden");
                                                    break;
                                                default:
                                                    throw new NotSupportedException();
                                            }

                                            _testOutput.WriteLine(responseMessage.ToString());
                                        }
                                    }
                                }

                                responseIdsToAcknowledge.Add(packageInfo.ResponseId);
                            }
                        }
                    }
                    break;
                }

                var currentTime = DateTimeOffset.UtcNow;
                var currentSpan = currentTime - startTime;
                if (currentSpan > maxResponseTime)
                    throw new Exception("Timeout for the response");

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }

            if (responseIdsToAcknowledge.Count != 0)
            {
                _testOutput.WriteLine("Bestätigen der Abholung für {0}", string.Join(", ", responseIdsToAcknowledge));
                client.ExecuteAcknowledge(CancellationToken.None, responseIdsToAcknowledge.ToArray()).Wait();
            }
        }
    }
}
