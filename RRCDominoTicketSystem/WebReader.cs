using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RRCDominoTicketSystem
{
    class WebReader
    {

        public static string Read(string code)
        {

            WebClient client = new WebClient();
            string htmlCode = client.DownloadString(SecretClass.GetURL(code));

            htmlCode = htmlCode.Replace("<pre></pre>", "");

            return htmlCode;

        }

    }
}
