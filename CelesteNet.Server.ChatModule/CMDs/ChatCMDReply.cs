﻿using Celeste.Mod.CelesteNet.DataTypes;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteNet.Server.Chat.CMDs {

    public class ChatCMDR : ChatCMDReply {
        public override string Info => $"Alias for {Chat.Settings.CommandPrefix}{Chat.Commands.Get<ChatCMDReply>().ID}";
    }

    public class ChatCMDReply : ChatCMD {
        public override string Args => "<text>";

        public override string Info => "Reply to the most recent whisper.";

        public override string Help =>
$@"Reply to the most recent whisper.
Sends a whisper to the most recent person who whispered to you, or who you whispered to.";

        public override void Run(ChatCMDEnv env, List<ChatCMDArg> args) {
            if (env.Session == null)
                return;

            if (env.Session.LastWhisperSessionID == uint.MaxValue)
                throw new Exception("You have not sent or received any whispers.");

            if (args.Count == 0)
                throw new Exception("No text.");

            args.Insert(0, new ChatCMDArg(env).Parse(env.Session.LastWhisperSessionID.ToString(), 0));

            Chat.Commands.Get<ChatCMDWhisper>().Run(env, args);
        }
    }
}

