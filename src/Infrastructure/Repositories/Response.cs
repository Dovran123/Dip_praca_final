using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Models;
using Domain.Entities;

namespace Infrastructure.Repositories;
public class Response
{

    public static List<APISprava> VytvorChybovuOdpoved(string kod, string text)
    {
        return new List<APISprava> {
                {
                    new APISprava { Zavaznost = ZavaznostSpravy.CHYBA, Kod = kod, Text = text }
                }
       };
    }
    public static List<APISprava> VytvorInfoOdpoved(string kod, string text, List<string> liekyKody)
    {
        var spravy = new List<APISprava>
    {
        new APISprava { Zavaznost = ZavaznostSpravy.INFO, Kod = kod, Text = text }
    };

        if (liekyKody.Any())
        {
            string liekyText = $"Týka sa liekov: {string.Join(", ", liekyKody)}";
            spravy.Add(new APISprava { Zavaznost = ZavaznostSpravy.INFO, Kod = "LIEKY_INFO", Text = liekyText });
        }

        return spravy;
    }

    public static List<APISprava> VytvorOdmietnutieOdpoved(string kod, string text)
    {
        return new List<APISprava>
        {
            new APISprava
            {
            Zavaznost = ZavaznostSpravy.ODMIETNUTIE,
            Kod = kod,
            Text = text
            }
        };
    }
}
