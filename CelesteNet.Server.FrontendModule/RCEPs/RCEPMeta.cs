﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CelesteNet.Server.Chat;
using Newtonsoft.Json;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

#if NETCORE
using System.Runtime.Loader;
#endif

namespace Celeste.Mod.CelesteNet.Server.Control {
    public static partial class RCEndpoints {

        [RCEndpoint(false, "/", null, null, "Root", "Main page.")]
        public static void Root(Frontend f, HttpRequestEventArgs c) {
            f.RespondContent(c, "frontend/main.html");
        }

        [RCEndpoint(false, "/auth", null, null, "Authenticate", "Basic POST authentication endpoint.")]
        public static void Auth(Frontend f, HttpRequestEventArgs c) {
            string? key = c.Request.Cookies[Frontend.COOKIE_SESSION]?.Value;
            string? pass;

            try {
                using (StreamReader sr = new StreamReader(c.Request.InputStream, Encoding.UTF8, false, 1024, true))
                using (JsonTextReader jtr = new JsonTextReader(sr))
                    pass = f.Serializer.Deserialize<string>(jtr);
            } catch (Exception e) {
                Logger.Log(LogLevel.DEV, "frontend-auth", e.ToString());
                pass = null;
            }

            if (string.IsNullOrEmpty(pass) && !key.IsNullOrEmpty()) {
                if (f.CurrentSessionKeys.Contains(key)) {
                    f.RespondJSON(c, new {
                        Key = key,
                        Info = "Resumed previous session based on cookies."
                    });
                    return;

                } else {
                    f.RespondJSON(c, new {
                        Error = "Previous session expired."
                    });
                    return;
                }
            }

            if (pass == null) {
                f.RespondJSON(c, new {
                    Error = "Invalid data."
                });
                return;
            }

            if (pass == f.Settings.Password) {
                key = Guid.NewGuid().ToString();
                f.CurrentSessionKeys.Add(key);
                c.Response.SetCookie(new Cookie(Frontend.COOKIE_SESSION, key));
                f.RespondJSON(c, new {
                    Key = key
                });
                return;
            }

            f.RespondJSON(c, new {
                Error = "Incorrect password."
            });
        }

        [RCEndpoint(false, "/ws", null, null, "WebSocket Connection", "Establish a WebSocket control panel connection.")]
        public static void WSPseudo(Frontend f, HttpRequestEventArgs c) {
            f.RespondJSON(c, new {
                Error = "Connect to this endpoint using WebSockets, not plain HTTP."
            });
        }

        [RCEndpoint(false, "/res", "?id={id}", "?id=frontend/css/frontend.css", "Resource", "Obtain a resource.")]
        public static void Resource(Frontend f, HttpRequestEventArgs c) {
            NameValueCollection args = f.ParseQueryString(c.Request.RawUrl);

            string id = args["id"];
            if (string.IsNullOrEmpty(id)) {
                c.Response.StatusCode = (int) HttpStatusCode.NotFound;
                f.RespondJSON(c, new {
                    Error = "No id specified."
                });
                return;
            }

            f.RespondContent(c, id);
        }

        [RCEndpoint(true, "/shutdown", null, null, "Shutdown", "Shut the server down.")]
        public static void Shutdown(Frontend f, HttpRequestEventArgs c) {
            f.RespondJSON(c, "OK");
            f.Server.IsAlive = false;
        }

        [RCEndpoint(false, "/eps", null, null, "Endpoint List", "List of all registered endpoints.")]
        public static void EPs(Frontend f, HttpRequestEventArgs c) {
            f.RespondJSON(c, f.EndPoints);
        }

        [RCEndpoint(true, "/asms", null, null, "Assembly List", "List of all loaded assemblies.")]
        public static void ASMs(Frontend f, HttpRequestEventArgs c) {
            f.RespondJSON(c, AppDomain.CurrentDomain.GetAssemblies().Select(asm => new {
                asm.GetName().Name,
                Version = asm.GetName().Version?.ToString() ?? "",
                Context =
#if NETCORE
                    (AssemblyLoadContext.GetLoadContext(asm) ?? AssemblyLoadContext.Default)?.Name ?? "Unknown",
#else
                    AppDomain.CurrentDomain.FriendlyName
#endif
            }).ToList());
        }

        [RCEndpoint(false, "/status", null, null, "Server Status", "Basic server status information.")]
        public static void Status(Frontend f, HttpRequestEventArgs c) {
            f.RespondJSON(c, new {
                Connections = f.Server.Connections.Count,
                PlayersByCon = f.Server.PlayersByCon.Count,
                PlayersByID = f.Server.PlayersByID.Count,
                PlayerRefs = f.Server.Data.GetRefs<DataPlayerInfo>().Length
            });
        }

        [RCEndpoint(false, "/players", null, null, "Player List", "Basic player list.")]
        public static void Players(Frontend f, HttpRequestEventArgs c) {
            f.RespondJSON(c, f.Server.PlayersByID.Values.Select(p => new {
                p.ID, p.PlayerInfo?.Name, p.PlayerInfo?.FullName,
                Connection = f.IsAuthorized(c) ? p.Con.ID : null
            }).ToArray());
        }

        [RCEndpoint(false, "/chatlog", "?count={count}", "?count=20", "Chat Log", "Basic chat log.")]
        public static void ChatLog(Frontend f, HttpRequestEventArgs c) {
            bool auth = f.IsAuthorized(c);
            NameValueCollection args = f.ParseQueryString(c.Request.RawUrl);

            if (!int.TryParse(args["count"], out int count) || count <= 0)
                count = 20;
            if (!auth && count > 100)
                count = 100;

            ChatModule chat = f.Server.Get<ChatModule>();
            List<object> log = new List<object>();
            lock (chat.ChatLog) {
                RingBuffer<DataChat> buffer = chat.ChatBuffer;
                for (int i = Math.Max(-chat.ChatBuffer.Moved, -count); i < 0; i++) {
                    DataChat msg = buffer[i];
                    if (msg.Target == null || auth)
                        log.Add(msg.ToFrontendChat());
                }
            }

            f.RespondJSON(c, log);
        }

    }
}