// <copyright file="IHttpClientFactory.cs" company="DATALINE GmbH &amp; Co. KG">
// Copyright (c) DATALINE GmbH &amp; Co. KG. All rights reserved.
// </copyright>

using System.Net.Http;

namespace Dsrv.KomServer.Vsnr
{
    /// <summary>
    /// Interface für eine Factory über die ein <see cref="HttpClient"/> erstellt wird
    /// </summary>
    public interface IHttpClientFactory
    {
        /// <summary>
        /// Erstellen eines <see cref="HttpClient"/>, der für die Versicherungsnummernabfrage verwendet werden soll
        /// </summary>
        /// <returns>Ein <see cref="HttpClient"/>, der für die Versicherungsnummernabfrage verwendet wird</returns>
        HttpClient CreateHttpClient();
    }
}
