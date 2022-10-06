using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;

namespace GravityDontFlipScreen
{
    public class GravityFlipSystem : ModSystem
    {
        internal static float gravDir;

        public override void Load() {
            // placing tiles
            IL.Terraria.Player.Update += Player_Update;
            // smart cursor
            On.Terraria.Player.SmartInteractLookup += Player_SmartInteractLookup;
            On.Terraria.GameContent.SmartCursorHelper.SmartCursorLookup += SmartCursorHelper_SmartCursorLookup;
            // making cursor direct to the correct position
            On.Terraria.Player.ItemCheck += Player_ItemCheck;
            On.Terraria.Player.QuickGrapple += Player_QuickGrapple;
            // all things draw normally
            Main.QueueMainThreadAction(() =>
            {
                On.Terraria.Main.DoDraw += Main_DoDraw;
            });
            // turn player back up-side down when drawing
            On.Terraria.Graphics.Renderers.LegacyPlayerRenderer.DrawPlayerFull += LegacyPlayerRenderer_DrawPlayerFull;
        }

        private void Player_SmartInteractLookup(On.Terraria.Player.orig_SmartInteractLookup orig, Player self) {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1) {
                orig.Invoke(self);
                return;
            }
            self.gravDir = 1;
            orig.Invoke(self);
            self.gravDir = -1;
        }

        private void SmartCursorHelper_SmartCursorLookup(On.Terraria.GameContent.SmartCursorHelper.orig_SmartCursorLookup orig, Player player) {
            if (player.whoAmI != Main.myPlayer || player.gravDir != -1) {
                orig.Invoke(player);
                return;
            }
            player.gravDir = 1;
            orig.Invoke(player);
            player.gravDir = -1;
        }

        private void Player_ItemCheck(On.Terraria.Player.orig_ItemCheck orig, Player self, int i) {
            if (i != Main.myPlayer || self.gravDir != -1) {
                orig.Invoke(self, i);
                return;
            }
            // This is vanilla MouseWorld code:
            // if (self.gravDir == -1f) result.Y = screenPosition.Y + (float)screenHeight - (float)mouseY;
            int mouseY = Main.mouseY;
            Main.mouseY = Main.screenHeight - Main.mouseY;
            orig.Invoke(self, i);
            Main.mouseY = mouseY;
        }

        private void Player_QuickGrapple(On.Terraria.Player.orig_QuickGrapple orig, Player self) {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1) {
                orig.Invoke(self);
                return;
            }
            self.gravDir = 1;
            orig.Invoke(self);
            self.gravDir = -1;
        }

        //Player.tileTargetX = (int) (((float) Main.mouseX + Main.screenPosition.X) / 16f);
        //Player.tileTargetY = (int) (((float) Main.mouseY + Main.screenPosition.Y) / 16f);
        //if (this.gravDir == -1f) {
        //    Player.tileTargetY = (int)((Main.screenPosition.Y + (float)Main.screenHeight - (float)Main.mouseY) / 16f);
        //}
        //IL code here:
        //IL_1D51: conv.i4
        //IL_1D52: stsfld int32 Terraria.Player::tileTargetY
        //IL_1D57: ldarg.0
        //IL_1D58: ldfld float32 Terraria.Player::gravDir
        //IL_1D5D: ldc.r4    -1
        //IL_1D62: bne.un.s IL_1D88
        private void Player_Update(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                i => i.Match(OpCodes.Conv_I4),
                i => i.MatchStsfld(typeof(Player), nameof(Player.tileTargetY)),
                i => i.Match(OpCodes.Ldarg_0),
                i => i.MatchLdfld(typeof(Player), nameof(Player.gravDir))
            );

            c.EmitDelegate<Func<float, float>>((_) => {
                return 1; // always 1 so placing tile position will be normal
            });
        }

        private void LegacyPlayerRenderer_DrawPlayerFull(On.Terraria.Graphics.Renderers.LegacyPlayerRenderer.orig_DrawPlayerFull orig, Terraria.Graphics.Renderers.LegacyPlayerRenderer self, Terraria.Graphics.Camera camera, Player drawPlayer) {
            if (drawPlayer.whoAmI != Main.myPlayer) {
                orig.Invoke(self, camera, drawPlayer);
                return;
            }
            drawPlayer.gravDir = gravDir;
            orig.Invoke(self, camera, drawPlayer);
            drawPlayer.gravDir = 1;
        }

        private void Main_DoDraw(On.Terraria.Main.orig_DoDraw orig, Main self, GameTime gameTime) {
            gravDir = Main.LocalPlayer.gravDir;
            Main.LocalPlayer.gravDir = 1;
            orig.Invoke(self, gameTime);
            Main.LocalPlayer.gravDir = gravDir;
        }
    }
}
