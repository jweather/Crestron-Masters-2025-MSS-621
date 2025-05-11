using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.WebScripting;
using Newtonsoft.Json;

namespace Masters_2025_MSS_621_JW {
    public class NvxProducer {
        private string configFile;

        HttpCwsServer server;
        public List<NvxSource> sources = new List<NvxSource>();

        // events
        public delegate void SourcesChangedHandler();
        public event SourcesChangedHandler SourcesChanged;


        public NvxProducer() {
            // try to load source file
            configFile = System.IO.Path.Combine(Directory.GetApplicationRootDirectory(), "User", "sources.json");
            try {
                using (var sr = File.OpenText(configFile)) {
                    string json = sr.ReadToEnd();
                    sources = JsonConvert.DeserializeObject<List<NvxSource>>(json);
                };
            } catch (Exception e) {
                CrestronConsole.PrintLine("NvxProducer load: " + e.Message);
                sources = new List<NvxSource>();
                sources.Add(new NvxSource("Default Source", "0.0.0.0"));
            }


            // web setup
            server = new HttpCwsServer("/nvx");
            server.ReceivedRequestEvent += Server_ReceivedRequestEvent;
            server.Register();
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;
        }

        private void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType) {
            if (programEventType == eProgramStatusEventType.Stopping) {
                if (server != null) {
                    try {
                        server.Unregister();
                        server.Dispose();
                    } catch { }
                }
            }
        }

        private void Server_ReceivedRequestEvent(object sender, HttpCwsRequestEventArgs args) {
            var Request = args.Context.Request;
            var Response = args.Context.Response;
            try {
                if (Request.Path == "/nvx/add") {
                    using (var reader = new StreamReader(Request.InputStream)) {
                        var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
                        addSource(obj["name"], obj["ip"]);
                    }

                } else if (Request.Path.StartsWith("/nvx/delete/")) {
                    string name = Request.Path.Replace("/nvx/delete/", "");
                    deleteSource(name);
                }
                // all requests return current list in JSON
                string json = JsonConvert.SerializeObject(sources);
                args.Context.Response.ContentType = "text/json";
                args.Context.Response.Write(json, true);
            } catch (Exception e) {
                ErrorLog.Error("Error in DefaultRequestHandler: {0}", e.Message);
            }
        }

        private void addSource(string name, string ip) {
            var source = new NvxSource(name, ip);
            sources.Add(source);
            touchSources();
        }

        private void deleteSource(string name) {
            NvxSource src = sources.Find(s => s.name == name);
            if (src != null) {
                sources.Remove(src);
                try { SourcesChanged?.Invoke(); } catch { }
            }
            touchSources();
        }

        private void touchSources() {
            try { SourcesChanged?.Invoke(); } catch { }
            try {
                string json = JsonConvert.SerializeObject(sources);
                using (var sw = File.Open(configFile, FileMode.Create)) {
                    sw.Write(json, Encoding.UTF8);
                }
            } catch { }
        }
    }

    public class NvxSource {
        public string name, ip;
        public NvxSource(string name, string ip) {
            this.name=name;
            this.ip=ip;
        }
    }
}
