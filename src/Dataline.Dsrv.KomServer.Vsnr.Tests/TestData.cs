using System;
using System.Collections.Generic;

using Dataline.Entities;

namespace Dsrv.KomServer.Vsnr.Tests
{
    public static class TestData
    {
        public static readonly IReadOnlyCollection<Person> TestPersons = new[]
        {
            new Person()
            {
                Id = 1,
                Personalnummer = "01",
                Nachname = "Presley",
                Vorname = "Elvis",
                PLZ = "12345",
                Ort = "Berlin",
                Gebdat = new DateTime(1930, 5, 6),
                IstMann = true,
            },
            new Person()
            {
                Id = 2,
                Personalnummer = "02",
                Nachname = "Yang",
                Vorname = "Paula",
                PLZ = "12345",
                Ort = "Berlin",
                GeburtsOrt = "Hofmann",
                Gebdat = new DateTime(1973, 4, 11),
                IstMann = false,
            },
            new Person()
            {
                Id = 3,
                Personalnummer = "03",
                Nachname = "Young",
                Vorname = "Paula",
                PLZ = "12345",
                Ort = "Berlin",
                GeburtsOrt = "Schleicher",
                Gebdat = new DateTime(1973, 4, 11),
                IstMann = false,
            },
            new Person()
            {
                Id = 4,
                Personalnummer = "04",
                Nachname = "Vogel",
                Vorname = "Joseph",
                PLZ = "12345",
                Ort = "Berlin",
                GeburtsOrt = "Unterföhring",
                Gebdat = new DateTime(1950, 3, 13),
                IstMann = true,
            },
            new Person()
            {
                Id = 5,
                Personalnummer = "05",
                Nachname = "Vogel",
                Vorname = "Franz-Josef",
                PLZ = "12345",
                Ort = "Berlin",
                GeburtsOrt = "München",
                Gebdat = new DateTime(1950, 3, 13),
                IstMann = true,
            }
        };
    }
}
