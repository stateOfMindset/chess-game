using System;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;

namespace ProjectV1
{
    public class ChessHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            try
            {
                string jsonString;
                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    jsonString = reader.ReadToEnd();
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var data = serializer.Deserialize<MoveRequest>(jsonString);

                ChessEngine engine = new ChessEngine(data.fen);
                string bestMove = engine.GetBestMove();

                var response = new { move = bestMove };
                context.Response.Write(serializer.Serialize(response));
            }
            catch (Exception ex)
            {
                context.Response.Write("{\"error\": \"" + ex.Message + "\"}");
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public class MoveRequest
        {
            public string fen { get; set; }
        }
    }
}