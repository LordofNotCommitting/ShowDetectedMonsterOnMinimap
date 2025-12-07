using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace ShowDetectedMonsterOnMinimap
{
    [HarmonyPatch(typeof(FogOfWar), nameof(FogOfWar.RefreshMinimap))]
    public static class MapMarkEnemy
    {
        static Sprite _minimapAllySprite;
        [HarmonyPostfix]
        public static void Postfix(ref FogOfWar __instance, bool forceShowMonsters = false, bool forceShowItems = false, bool forceShowExits = false)
        {
            if (!forceShowMonsters)
            {

                if (__instance._creatures.Player != null)
                {

                    bool reveal_all = __instance._creatures.Player.CreatureData.EffectsController.HasAnyEffect<WoundEffectRevealingEye>();
                    if (!reveal_all && __instance._creatures.Player.IsAbleToSpotAnEnemy()) {
                        int enemyRevealRange = __instance._creatures.Player.CreatureData.GetEnemyRevealRange();
                        CellPosition temp_map_player_pos = new CellPosition(__instance._creatures.Player.CreatureData.Position.X * 4, __instance._creatures.Player.CreatureData.Position.Y * 4);
                        //Plugin.Logger.Log("radius activated, seek rad is " + enemyRevealRange);
                        for (int k = 0; k < __instance._textureHeight; k++)
                        {
                            for (int l = 0; l < __instance._textureWidth; l++)
                            {
                                MapCell cell2 = __instance._mapGrid.GetCell(l, k, true);
                                CellPosition position2 = new CellPosition(l * 4, k * 4);
                                CellPosition invis_cell = new CellPosition(l, k);
                                // (__instance._creatures.Player.CreatureData.Position.Distance(invis_cell) <= (float)(enemyRevealRange))
                                float reveal_range_edge = ((float)(enemyRevealRange) - (float)__instance._creatures.Player.CreatureData.Position.Distance(invis_cell));
                                if ((cell2.GetTileIndex(1) != 255) && (reveal_range_edge >= 0 && reveal_range_edge <= 1))
                                {
                                    /*
                                    NativeArray<Color32> pixelData = __instance._mapTexture.GetPixelData<Color32>(0);
                                    Color32 temp_color = TextureHelper.GetColor(ref pixelData, __instance._mapTexture, position2.X, position2.Y);

                                    Color32 override_color = new Color32(temp_color.r, (byte)(Math.Min(temp_color.g + (byte)70, byte.MaxValue)), temp_color.b, temp_color.a);

                                    if (!cell2.IsExplored && !cell2.isSeen)
                                    {
                                        TextureHelper.FillWithColorTo32(TextureHelper.FillMode.Rewrite, __instance._mapTexture, override_color, position2, 4, 4, false);
                                    }
                                    else if (cell2.IsExplored && !cell2.isSeen)
                                    {
                                        TextureHelper.FillWithColorTo32(TextureHelper.FillMode.Rewrite, __instance._mapTexture, override_color, position2, 4, 4, false);
                                    }
                                    */

                                    NativeArray<Color32> pixelData = __instance._mapTexture.GetPixelData<Color32>(0);
                                    Color32 temp_color;
                                    Color32 override_color;
                                    int width2 = __instance._mapTexture.width;
                                    int height2 = __instance._mapTexture.height;
                                    for (int i = 0; i < 4; i++)
                                    {
                                        for (int j = 0; j < 4; j++)
                                        {
                                            int num = position2.X + i;
                                            int num2 = position2.Y + j;
                                            if (num >= 0 && num < width2 && num2 >= 0 && num2 < height2)
                                            {
                                                //do I need to blend color?
                                                temp_color = __instance._mapTexture.GetPixel(num, num2);
                                                override_color = new Color32(temp_color.r, (byte)(Math.Min(temp_color.g + (byte)70, byte.MaxValue)), temp_color.b, temp_color.a);
                                                TextureHelper.SetColor(ref pixelData, __instance._mapTexture, num, num2, override_color);
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
                if (_minimapAllySprite is null) {

                    //Plugin.Logger.Log("when does image creation proc really");
                    Sprite original = __instance._minimapEnemySprite;

                    Texture2D tex = original.texture;
                    Texture2D newTex = new Texture2D(tex.width, tex.height);

                    Color[] px = tex.GetPixels();
                    for (int i = 0; i < px.Length; i++)
                    {
                        Color32 temp_color = px[i];
                        px[i] = new Color32(0, (byte)(Math.Min(temp_color.r + (byte)70, byte.MaxValue)), (byte)(Math.Min(temp_color.r + (byte)70, byte.MaxValue)), temp_color.a); // white RGB, keep alpha
                    }

                    newTex.SetPixels(px);
                    newTex.Apply();

                    _minimapAllySprite = Sprite.Create(
                        newTex,
                        __instance._minimapEnemySprite.textureRect,
                        __instance._minimapEnemySprite.pivot
                    );
                }
                





                foreach (Monster creature in __instance._creatures.Monsters)
                {

                    int x3 = creature.CreatureData.Position.X * 4;
                    int y3 = creature.CreatureData.Position.Y * 4;
                    if (creature.ShowSignal || creature._wasSpottedThisAp || creature.CreatureData.EffectsController.HasAnyEffect<Spotted>())
                    {
                        //unseen hostile
                        if (!creature.IsSeenByPlayer)
                        {
                            bool is_quest = MissionSystem.IsQuestMonster(__instance._raidMetadata, creature);

                            if (!is_quest && (creature.CreatureData.CreatureAlliance != __instance._creatures.Player.CreatureData.CreatureAlliance))
                            {
                                TextureHelper.BakeSprite32To32(__instance._mapTexture, __instance._minimapEnemySprite, new CellPosition(x3, y3), false);
                            }
                        }
                        //unseen ally
                        if (creature.CreatureData.CreatureAlliance == __instance._creatures.Player.CreatureData.CreatureAlliance)
                        {
                            //Plugin.Logger.Log("player ally detected");
                            TextureHelper.BakeSprite32To32(__instance._mapTexture, _minimapAllySprite, new CellPosition(x3, y3), false);
                        }
                    } 
                    //if creature is seen and is ally
                    else if (creature.IsSeenByPlayer && creature.CreatureData.CreatureAlliance == __instance._creatures.Player.CreatureData.CreatureAlliance)
                    {
                        //Plugin.Logger.Log("player ally detected");
                        TextureHelper.BakeSprite32To32(__instance._mapTexture, _minimapAllySprite, new CellPosition(x3, y3), false);
                    }



                }
                __instance._mapTexture.Apply();
            }


        }
    }
}



