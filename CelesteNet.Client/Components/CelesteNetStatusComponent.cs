﻿using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteNet.Client.Components
{
    public class CelesteNetStatusComponent : CelesteNetGameComponent {

        private bool show;
        private MTexture? texture;
        private string? text;
        private float time;
        private float timeSpin;
        private const float timeIn = 0.3f;
        private const float timeOut = 0.15f;

        private float timeText;
        private const float timeTextMax = 100f;

        private bool spin = false;
        private float spinSpeed = 0f;

        public string? Text => show ? text : null;
        public float Time => show ? timeTextMax : timeText;
        public bool Spin => spin;
        public bool IsVisible => show;

        public CelesteNetStatusComponent(CelesteNetClientContext context, Game game)
            : base(context, game) {

            UpdateOrder = 10000;
            DrawOrder = 10200;

            Persistent = true;
            AutoDispose = false;
            Enabled = true;
        }

        protected override void LoadContent() {
            base.LoadContent();

            texture = GFX.Gui["reloader/cogwheel"];
        }

        public void Handle(CelesteNetConnection con, DataServerStatus status) {
            Set(status.Text, status.Time > 0f ? status.Time : 3f, status.Spin);
        }

        public void Handle(CelesteNetConnection con, DataDisconnectReason reason) {
            Set(reason.Text, 8f, spin: false, dcReason: false);
        }

        public void Set(string text, float timeText = timeTextMax, bool spin = true, bool dcReason = true) {
            if (string.IsNullOrEmpty(text)) {
                show = false;
                return;
            }

            DataDisconnectReason? reason = CelesteNetClientModule.Instance.lastDisconnectReason;

            if (dcReason && reason != null && !reason.Text.IsNullOrEmpty()) {
                text += $" ({reason.Text})";
            }

            this.text = text;
            this.timeText = timeText;
            this.spin = spin;
            show = true;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (show && 0f < timeText && timeText <= 100f) {
                timeText -= Engine.RawDeltaTime;
                if (timeText <= 0f) {
                    timeText = 0f;
                    show = false;
                }
            }

            if (show || time != 0f) {
                time += Engine.RawDeltaTime;

                float timeMax = show ? timeIn : (timeIn + timeOut);
                if (time >= timeMax)
                    time = timeMax;

                if (!show && time >= timeMax)
                    time = 0f;
            }

            if (spin) {
                spinSpeed += Engine.RawDeltaTime * 4f;
                if (spinSpeed > 1f)
                    spinSpeed = 1f;
            } else {
                spinSpeed -= Engine.RawDeltaTime * 3f;
                if (spinSpeed < 0f)
                    spinSpeed = 0f;
            }

            timeSpin += Engine.RawDeltaTime * spinSpeed;

            if (Context == null && time == 0f)
                Dispose();
        }

        public override void Draw(GameTime? gameTime) {
            if (show || time != 0f)
                base.Draw(gameTime);
        }

        protected override void Render(GameTime? gameTime, bool toBuffer) {
            float a = Ease.SineInOut(show ? (time / timeIn) : (1f - (time - timeIn) / timeOut));

            Vector2 anchor = new(96f, UI_HEIGHT - 96f);

            Vector2 pos = anchor + new Vector2(0f, 0f);
            float cogScale = MathHelper.Lerp(0.2f, 0.25f, Ease.CubeOut(a));
            if (!(texture?.Texture?.Texture?.IsDisposed ?? true)) {
                float cogRot = timeSpin * 4f;
                for (int x = -2; x <= 2; x++)
                    for (int y = -2; y <= 2; y++)
                        if (x != 0 || y != 0)
                            texture.DrawCentered(pos + new Vector2(x, y), Color.Black * a * a * a * a, cogScale, cogRot);
                texture.DrawCentered(pos, Color.White * a, cogScale, cogRot);
            }

            pos = anchor + new Vector2(48f, 0f);
            string? text = this.text;
            if (!string.IsNullOrEmpty(text) && Dialog.Language != null && CelesteNetClientFont.Font != null) {
                Vector2 size = CelesteNetClientFont.Measure(text);
                CelesteNetClientFont.DrawOutline(text, pos + new Vector2(size.X * 0.5f, 0f), new(0.5f, 0.5f), Vector2.One * MathHelper.Lerp(0.8f, 1f, Ease.CubeOut(a)), Color.White * a, 2f, Color.Black * a * a * a * a);
            }
        }

    }
}
