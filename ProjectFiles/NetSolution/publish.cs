#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.CommunicationDriver;
using FTOptix.Alarm;
using FTOptix.CoreBase;
using FTOptix.CODESYS;
using FTOptix.S7TiaProfinet;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.Core;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
#endregion

public class Publish : BaseNetLogic
{
    private HttpListener listener;
    private Thread listenerThread;

    public override void Start()
    {
        listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8765/");
        listener.Start();
        listenerThread = new Thread(ListenLoop) { IsBackground = true };
        listenerThread.Start();
    }

    public override void Stop()
    {
        listener?.Stop();
    }

    private void ListenLoop()
    {
        while (listener.IsListening)
        {
            try
            {
                var ctx = listener.GetContext();
                if (ctx.Request.IsWebSocketRequest)
                    Task.Run(() => HandleWebSocket(ctx));
                else
                    ctx.Response.Close();
            }
            catch { }
        }
    }

    private async Task HandleWebSocket(HttpListenerContext ctx)
    {
        var wsCtx = await ctx.AcceptWebSocketAsync(null);
        var ws = wsCtx.WebSocket;

        while (ws.State == WebSocketState.Open)
        {
            try
            {
                string tag1Str = (((UAManagedCore.UAValue)Project.Current.GetVariable("Model/VariabiliRicetta/nomeFilePunti").Value).Value ?? "").ToString().Replace("\\", "\\\\");
              //  float tag2Val = Convert.ToSingle(((UAManagedCore.UAValue)Project.Current.GetVariable("Model/VariabiliRicetta/velX").Value).Value);
                //string tag2Str = tag2Val.ToString(System.Globalization.CultureInfo.InvariantCulture);

                var nPuntiNode = Project.Current.GetVariable("Model/VariabiliRicetta/nPunti");
                object raw = Project.Current.GetVariable("Model/Profilo/xyGraph").Value;
                float[,] punti = ((UAManagedCore.UAValue)raw).Value as float[,];
                int n = punti != null ? nPuntiNode.Value : 0;
                var pts = new string[n];
                for (int i = 0; i < n; i++)
                    pts[i] = "[" +
                        punti[i, 0].ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        punti[i, 1].ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";

                var json = $"{{\"nomeFilePunti\":\"{tag1Str}\",\"points\":[{string.Join(",", pts)}]}}";
                var buf = System.Text.Encoding.UTF8.GetBytes(json);
                await ws.SendAsync(new ArraySegment<byte>(buf),
                    WebSocketMessageType.Text, true, CancellationToken.None);

                await Task.Delay(2000);
            }
            catch { break; }
        }

        ws.Dispose();
    }
}
/*#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.CommunicationDriver;
using FTOptix.Alarm;
using FTOptix.CoreBase;
using FTOptix.CODESYS;
using FTOptix.S7TiaProfinet;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.Retentivity;
using FTOptix.EventLogger;
using FTOptix.Core;
using System.Net;
using System.Threading;
using FTOptix.S7TCP;


#endregion



public class Publish : BaseNetLogic
{
    private PeriodicTask periodicTask;
    private HttpListener listener;
    private Thread listenerThread;

    public override void Start()
    {
        listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8765/tags/");
        listener.Start();

        listenerThread = new Thread(ListenLoop) { IsBackground = true };
        listenerThread.Start();
    }

    public override void Stop()
    {
        listener?.Stop();
    }

    private void ListenLoop()
    {
        while (listener.IsListening)
        {
            try
            {
                var ctx = listener.GetContext();

                ctx.Response.Headers.Add("Access-Control-Allow-Origin",
                ctx.Request.Headers["Origin"] ?? "null");
                ctx.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                ctx.Response.Headers.Add("Access-Control-Allow-Headers", "*");

                if (ctx.Request.HttpMethod == "OPTIONS")
                {
                    ctx.Response.StatusCode = 204;
                    ctx.Response.Close();
                    continue;
                }


                var tag1 = Project.Current.GetVariable("Model/VariabiliRicetta/nomeFilePunti").Value;
                var tag2 = Project.Current.GetVariable("Model/VariabiliRicetta/velX").Value;

                object raw = Project.Current.GetVariable("Model/Profilo/xyGraph").Value;
                //float[,] punti = raw as float[,];
                float[,] punti = ((UAManagedCore.UAValue)raw).Value as float[,];
                int n = punti != null ? punti.GetLength(0) : 0;
                var pts = new string[n];
                for (int i = 0; i < n; i++)
                    pts[i] = "[" +
                        punti[i, 0].ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        punti[i, 1].ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";

                var json = $"{{\"nomeFilePunti\":{tag1},\"VelX\":{tag2},\"points\":[{string.Join(",", pts)}]}}";
                var buf = System.Text.Encoding.UTF8.GetBytes(json);

                ctx.Response.ContentType = "application/json";
                ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                ctx.Response.Close();
            }

            catch { }
        }
    }
}

*/
