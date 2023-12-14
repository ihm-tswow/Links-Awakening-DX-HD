using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.Editor;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Bosses;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.GameObjects.MidBoss;
using ProjectZ.InGame.GameObjects.NPCs;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects
{
    class GameObjectTemplates
    {
        public static Dictionary<string, GameObjectTemplate> ObjectTemplates = new Dictionary<string, GameObjectTemplate>();
        public static Dictionary<string, ObjActivator.ObjectActivator<GameObject>> ObjectSpawner = new Dictionary<string, ObjActivator.ObjectActivator<GameObject>>();
        public static Dictionary<string, ParameterInfo[]> GameObjectParameter = new Dictionary<string, ParameterInfo[]>();

        public static void SetUpGameObjects()
        {
            // collision boxes
            var colliderColor = Color.OrangeRed * 0.65f;
            ObjectTemplates.Add("c1", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal, new[] { new Rectangle(0, 0, 16, 16) } }));
            ObjectTemplates.Add("c2", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal, new[] { new Rectangle(0, 8, 16, 8) } }));
            ObjectTemplates.Add("c5", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal, new[] { new Rectangle(0, 0, 16, 8) } }));
            ObjectTemplates.Add("c3", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal, new[] { new Rectangle(0, 0, 8, 16) } }));
            ObjectTemplates.Add("c4", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal, new[] { new Rectangle(8, 0, 8, 16) } }));
            ObjectTemplates.Add("colliderL0", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal, new[] { new Rectangle(0, 8, 8, 8), new Rectangle(0, 0, 16, 8) } }));
            ObjectTemplates.Add("colliderL1", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal, new[] { new Rectangle(8, 8, 8, 8), new Rectangle(0, 0, 16, 8) } }));
            ObjectTemplates.Add("colliderL2", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal, new[] { new Rectangle(0, 0, 8, 8), new Rectangle(0, 8, 16, 8) } }));
            ObjectTemplates.Add("colliderL3", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal, new[] { new Rectangle(8, 0, 8, 8), new Rectangle(0, 8, 16, 8) } }));

            var lowerColliderColor = Color.Green * 0.65f;
            var lowerCollisionType = Values.CollisionTypes.Normal | Values.CollisionTypes.ThrowIgnore | Values.CollisionTypes.ThrowWeaponIgnore;
            ObjectTemplates.Add("lowCollider16", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(0, 0, 16, 16) } }));
            ObjectTemplates.Add("lowCollider0", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(0, 8, 16, 8) } }));
            ObjectTemplates.Add("lowCollider1", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(0, 0, 16, 8) } }));
            ObjectTemplates.Add("lowCollider2", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(0, 0, 8, 16) } }));
            ObjectTemplates.Add("lowCollider3", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(8, 0, 8, 16) } }));
            ObjectTemplates.Add("c13", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(0, 0, 8, 8) } }));
            ObjectTemplates.Add("c6", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(8, 0, 8, 8) } }));
            ObjectTemplates.Add("c7", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(0, 8, 8, 8) } }));
            ObjectTemplates.Add("c8", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(8, 8, 8, 8) } }));
            ObjectTemplates.Add("c9", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(0, 8, 8, 8), new Rectangle(0, 0, 16, 8) } }));
            ObjectTemplates.Add("c10", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(8, 8, 8, 8), new Rectangle(0, 0, 16, 8) } }));
            ObjectTemplates.Add("c11", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(0, 0, 8, 8), new Rectangle(0, 8, 16, 8) } }));
            ObjectTemplates.Add("c12", new GameObjectTemplate(typeof(ObjCollider), new object[] { lowerColliderColor, lowerCollisionType, new[] { new Rectangle(8, 0, 8, 8), new Rectangle(0, 8, 16, 8) } }));

            ObjectTemplates.Add("enemyWall", new GameObjectTemplate(typeof(ObjCollider), new object[] { 32, new Rectangle(0, 0, 16, 16), Values.CollisionTypes.NPCWall, -1 }));
            ObjectTemplates.Add("drownResetExclude", new GameObjectTemplate(typeof(ObjCollider), new object[] { 32, new Rectangle(0, 0, 16, 16), Values.CollisionTypes.DrownExclude, -1 }));
            ObjectTemplates.Add("hookshotGrip", new GameObjectTemplate(typeof(ObjCollider), new object[] { 32, new Rectangle(0, 0, 16, 16), Values.CollisionTypes.Hookshot, -1 }));

            ObjectTemplates.Add("lowerLevelCollider", new GameObjectTemplate(typeof(ObjCollider), new object[] { 32, new Rectangle(0, 0, 16, 16), Values.CollisionTypes.Normal, 0 }));
            ObjectTemplates.Add("lowerLevelCollider1", new GameObjectTemplate(typeof(ObjCollider), new object[] { 32, new Rectangle(0, 8, 16, 8), Values.CollisionTypes.Normal, 0 }));
            ObjectTemplates.Add("lowerLevelCollider2", new GameObjectTemplate(typeof(ObjCollider), new object[] { 32, new Rectangle(0, 0, 8, 16), Values.CollisionTypes.Normal, 0 }));
            ObjectTemplates.Add("colliderLevel1", new GameObjectTemplate(typeof(ObjCollider), new object[] { 32, new Rectangle(0, 0, 16, 16), Values.CollisionTypes.Normal, 1 }));
            ObjectTemplates.Add("raftCollider", new GameObjectTemplate(typeof(ObjCollider), new object[] { 32, new Rectangle(0, 0, 16, 16), Values.CollisionTypes.RaftExit, 1 }));

            ObjectTemplates.Add("c1PushIgnore", new GameObjectTemplate(typeof(ObjCollider), new object[] { colliderColor, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore, new[] { new Rectangle(0, 0, 16, 16) } }));

            ObjectTemplates.Add("oneWayBridge2", new GameObjectTemplate(typeof(ObjColliderOneWay), new object[] { new Rectangle(15, 0, 1, 16), Values.CollisionTypes.Normal, 2 }));
            ObjectTemplates.Add("oneWayBridge0", new GameObjectTemplate(typeof(ObjColliderOneWay), new object[] { new Rectangle(0, 0, 1, 16), Values.CollisionTypes.Normal, 0 }));


            ObjectTemplates.Add("break_collider_end", null);
            ObjectTemplates.Add("break_sprite_start", null);

            ObjectTemplates.Add("tree0", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_0", new Vector2(16, 24), Values.LayerPlayer, "tree_0_shadow", new Rectangle(-16, -20, 32, 28), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("treeWoods", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_7", new Vector2(16, 24), Values.LayerPlayer, "tree_0_shadow", new Rectangle(-16, -20, 32, 28), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("treeWoods2", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_6", new Vector2(16, 24), Values.LayerPlayer, "tree_0_shadow", new Rectangle(-16, -20, 32, 27), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("tree1", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_1", new Vector2(16, 24), Values.LayerPlayer, "tree_1_shadow", new Rectangle(-16, -20, 32, 28), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("tree2", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_2", new Vector2(16, 24), Values.LayerPlayer, "tree_2_shadow", new Rectangle(-16, -20, 32, 28), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("tree3", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_3", new Vector2(16, 24), Values.LayerPlayer, "tree_2_shadow", new Rectangle(-16, -20, 32, 28), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("tree4", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_4", new Vector2(16, 24), Values.LayerPlayer, "tree_4_shadow", new Rectangle(-16, -20, 32, 28), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("tree5", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_5", new Vector2(16, 24), Values.LayerPlayer, "tree_5_shadow", new Rectangle(-16, -20, 32, 28), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("stree", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_8", new Vector2(8, 24), Values.LayerPlayer, "tree_8_shadow", new Rectangle(-8, -8, 16, 16), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("phonehouse", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_phonehouse", new Vector2(24, 24), Values.LayerPlayer, "tree_phonehouse_shadow", new Rectangle(-24, -20, 48, 12), Values.CollisionTypes.Normal }));

            ObjectTemplates.Add("tree9", new GameObjectTemplate(typeof(ObjSprite), new object[] { "tree_9", new Vector2(16, 24), Values.LayerPlayer, "tree_9_shadow", new Rectangle(-16, -20, 32, 28), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("seashell_house", new GameObjectTemplate(typeof(ObjSprite), new object[] { "seashell_house", new Vector2(24, 24), Values.LayerPlayer, "seashell_house_shadow", new Rectangle(-24, -20, 48, 12), Values.CollisionTypes.Normal }));

            ObjectTemplates.Add("strandplant", new GameObjectTemplate(typeof(ObjSprite), new object[] { "strandPlant", new Vector2(8, 12), Values.LayerPlayer, "strandPlantShadow", new Rectangle(-8, -8, 15, 12), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("strandshell", new GameObjectTemplate(typeof(ObjSprite), new object[] { "strandShell", new Vector2(8, 12), Values.LayerPlayer, "strandShellShadow" }));
            ObjectTemplates.Add("dungeonOneStatue", new GameObjectTemplate(typeof(ObjSprite), new object[] { "dungeonOneStatue", new Vector2(8, 28), Values.LayerPlayer, "dungeonOneStatue", new Rectangle(-8, -12, 16, 16), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("dungeonOneStatueKey", new GameObjectTemplate(typeof(ObjSprite), new object[] { "dungeonOneStatueKey", new Vector2(8, 28), Values.LayerPlayer, "dungeonOneStatueKey", new Rectangle(-8, -12, 16, 16), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("dungeonStatue", new GameObjectTemplate(typeof(ObjSprite), new object[] { "dungeonStatue_0", new Vector2(8, 13), Values.LayerPlayer, "dungeonStatue_0", new Rectangle(-8, -10, 16, 13), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("dungeonStatueGrey", new GameObjectTemplate(typeof(ObjSprite), new object[] { "dungeonStatue_1", new Vector2(8, 13), Values.LayerPlayer, "dungeonStatue_1", new Rectangle(-8, -10, 16, 13), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("dungeonStatueD7", new GameObjectTemplate(typeof(ObjSprite), new object[] { "dungeonStatue_2", new Vector2(8, 13), Values.LayerPlayer, "dungeonStatue_2", new Rectangle(-8, -10, 16, 13), Values.CollisionTypes.Normal }));

            ObjectTemplates.Add("caveFairyStatue", new GameObjectTemplate(typeof(ObjSprite), new object[] { "caveFairyStatue", new Vector2(8, 28), Values.LayerPlayer, "caveFairyStatue", new Rectangle(-8, -8, 16, 12), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("gravejardfence", new GameObjectTemplate(typeof(ObjSprite), new object[] { "gravejardFence", new Vector2(7, 12), Values.LayerPlayer, "gravejardFence", new Rectangle(-7, -8, 15, 12), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("desertpillar", new GameObjectTemplate(typeof(ObjSprite), new object[] { "desertPillar", new Vector2(8, 28), Values.LayerPlayer, "desertPillar", new Rectangle(-8, -20, 15, 24), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("phone", new GameObjectTemplate(typeof(ObjSprite), new object[] { "phone", new Vector2(8, 14), Values.LayerPlayer, "phone", new Rectangle(-8, -10, 16, 12), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("itemShop", new GameObjectTemplate(typeof(ObjSprite), new object[] { "itemShop", new Vector2(8, 14), Values.LayerPlayer, "itemShop", new Rectangle(-8, -10, 16, 12), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("armosStatue", new GameObjectTemplate(typeof(ObjSprite), new object[] { "armos", new Vector2(8, 14), Values.LayerPlayer, "armos", new Rectangle(-8, -10, 16, 12), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("armosDarkStatue", new GameObjectTemplate(typeof(ObjSprite), new object[] { "armos dark", new Vector2(8, 14), Values.LayerPlayer, "armos dark", new Rectangle(-8, -10, 16, 12), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("statueCastle", new GameObjectTemplate(typeof(ObjSprite), new object[] { "statueCastle", new Vector2(8, 14), Values.LayerPlayer, "statueCastle", new Rectangle(-8, -10, 16, 12), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("dungeon3Head", new GameObjectTemplate(typeof(ObjSprite), new object[] { "dungeon3Head", new Vector2(8, 12), Values.LayerPlayer, "dungeon3Head", new Rectangle(-8, -8, 16, 12), Values.CollisionTypes.Normal }));

            ObjectTemplates.Add("banana", new GameObjectTemplate(typeof(ObjSprite), new object[] { "bananas", new Vector2(8, 16), Values.LayerPlayer, "bananas", new Rectangle(-8, -14, 16, 14), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("npc_bag", new GameObjectTemplate(typeof(ObjSprite), new object[] { "npc_bag", new Vector2(8, 14), Values.LayerPlayer, "npc_bag", new Rectangle(-7, -8, 14, 10), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("statueD3", new GameObjectTemplate(typeof(ObjSprite), new object[] { "statue_d3", new Vector2(8, 30), Values.LayerPlayer, "statue_d3", new Rectangle(-8, -14, 16, 16), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("statueD3Key", new GameObjectTemplate(typeof(ObjSprite), new object[] { "statue_d3_key", new Vector2(8, 30), Values.LayerPlayer, "statue_d3_key", new Rectangle(-8, -14, 16, 16), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("statueMermaid", new GameObjectTemplate(typeof(ObjSprite), new object[] { "statue_mermaid", new Vector2(8, 28), Values.LayerPlayer, "statue_mermaid", new Rectangle(-8, -14, 16, 14), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("mountainStone", new GameObjectTemplate(typeof(ObjSprite), new object[] { "stone_mountain_0", new Vector2(8, 13), Values.LayerPlayer, "stone_mountain_0", new Rectangle(-8, -11, 16, 12), Values.CollisionTypes.Normal | Values.CollisionTypes.Hookshot }));
            ObjectTemplates.Add("dungeon7_keyhole", new GameObjectTemplate(typeof(ObjSprite), new object[] { "dungeon7_keyhole", new Vector2(8, 14), Values.LayerPlayer, "dungeon7_keyhole", new Rectangle(-8, -12, 16, 14), Values.CollisionTypes.Normal }));

            ObjectTemplates.Add("overworldDonut", new GameObjectTemplate(typeof(ObjSprite), new object[] {
                "overworldDonut", new Vector2(8, 0), Values.LayerPlayer, "overworldDonut", new Rectangle(-8, 0, 16, 16), Values.CollisionTypes.Normal | Values.CollisionTypes.Hookshot }));

            ObjectTemplates.Add("cave_table", new GameObjectTemplate(typeof(ObjSprite), new object[] { "cave_table", new Vector2(0, 1), Values.LayerPlayer, null, new Rectangle(0, -1, 32, 16), Values.CollisionTypes.Normal | Values.CollisionTypes.ThrowWeaponIgnore }));
            ObjectTemplates.Add("cave_bed", new GameObjectTemplate(typeof(ObjSprite), new object[] { "cave_bed", new Vector2(0, 0), Values.LayerPlayer, null, new Rectangle(0, 0, 16, 32), Values.CollisionTypes.Normal | Values.CollisionTypes.ThrowWeaponIgnore }));

            ObjectTemplates.Add("vase_empty", new GameObjectTemplate(typeof(ObjSprite), new object[] { "vase_empty", new Vector2(8, 16), Values.LayerPlayer, "vase_empty" }));
            ObjectTemplates.Add("vase_flower", new GameObjectTemplate(typeof(ObjSprite), new object[] { "vase_flower", new Vector2(8, 16), Values.LayerPlayer, "vase_flower" }));

            ObjectTemplates.Add("painting", new GameObjectTemplate(typeof(ObjSprite), new object[] { "painting", new Vector2(8, 16), Values.LayerPlayer, "painting", new Rectangle(-8, -12, 16, 12), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("owl_statue", new GameObjectTemplate(typeof(ObjSprite), new object[] { "owl_statue", new Vector2(0, 12), Values.LayerPlayer, "owl_statue_shadow", new Rectangle(0, -12, 16, 16), Values.CollisionTypes.Normal }));
            ObjectTemplates.Add("photohouse_light", new GameObjectTemplate(typeof(ObjSprite), new object[] { "photohouse_light", new Vector2(0, 24), Values.LayerPlayer, "photohouse_light" }));

            ObjectTemplates.Add("castle_roof_0", new GameObjectTemplate(typeof(ObjSprite), new object[] { "castle_roof_0", new Vector2(0, 17), Values.LayerPlayer, null }));
            ObjectTemplates.Add("castle_roof_1", new GameObjectTemplate(typeof(ObjSprite), new object[] { "castle_roof_1", new Vector2(0, 17), Values.LayerPlayer, null }));
            ObjectTemplates.Add("castle_roof_2", new GameObjectTemplate(typeof(ObjSprite), new object[] { "castle_roof_2", new Vector2(0, 17), Values.LayerPlayer, null }));
            ObjectTemplates.Add("castle_roof_3", new GameObjectTemplate(typeof(ObjSprite), new object[] { "castle_roof_3", new Vector2(0, 0), Values.LayerPlayer, null }));
            ObjectTemplates.Add("castle_roof_4", new GameObjectTemplate(typeof(ObjSprite), new object[] { "castle_roof_4", new Vector2(0, 16), Values.LayerPlayer, null }));
            ObjectTemplates.Add("castle_roof_5", new GameObjectTemplate(typeof(ObjSprite), new object[] { "castle_roof_5", new Vector2(0, 16), Values.LayerPlayer, null }));

            // roofs
            ObjectTemplates.Add("roof01", new GameObjectTemplate(typeof(ObjSprite), new object[] { "roof_0", new Vector2(0, 18), Values.LayerPlayer, null }));
            ObjectTemplates.Add("roof02", new GameObjectTemplate(typeof(ObjSprite), new object[] { "roof_1", new Vector2(0, 18), Values.LayerPlayer, null }));
            ObjectTemplates.Add("roof03", new GameObjectTemplate(typeof(ObjSprite), new object[] { "roof_2", new Vector2(0, 18), Values.LayerPlayer, null }));
            ObjectTemplates.Add("roof04", new GameObjectTemplate(typeof(ObjSprite), new object[] { "roof_3", new Vector2(0, 16), Values.LayerPlayer, null }));
            ObjectTemplates.Add("roof05", new GameObjectTemplate(typeof(ObjSprite), new object[] { "roof_4", new Vector2(0, 18), Values.LayerPlayer, null }));
            ObjectTemplates.Add("roof06", new GameObjectTemplate(typeof(ObjSprite), new object[] { "roof_5", new Vector2(0, 18), Values.LayerPlayer, null }));

            ObjectTemplates.Add("d5_entry", new GameObjectTemplate(typeof(ObjSprite), new object[] { "d5_entry", Vector2.Zero, Values.LayerPlayer, "d5_entry_shadow" }));
            ObjectTemplates.Add("witch_house", new GameObjectTemplate(typeof(ObjSprite), new object[] { "witch_house", new Vector2(0, 30), Values.LayerPlayer, "witch_house_shadow" }));

            ObjectTemplates.Add("seashell_post", new GameObjectTemplate(typeof(ObjSprite), new object[] { "seashell_post", new Vector2(0, 0), Values.LayerTop, null }));

            ObjectTemplates.Add("stairsCastle", new GameObjectTemplate(typeof(ObjSprite), new object[] { "stairs_0", Vector2.Zero, Values.LayerBottom, null }));
            ObjectTemplates.Add("stairsWoods", new GameObjectTemplate(typeof(ObjSprite), new object[] { "stairs_1", Vector2.Zero, Values.LayerBottom, null }));
            ObjectTemplates.Add("dungeon_stairs", new GameObjectTemplate(typeof(ObjSprite), new object[] { "stairs_2", Vector2.Zero, Values.LayerBottom, null }));
            ObjectTemplates.Add("dungeon_6_stairs", new GameObjectTemplate(typeof(ObjSprite), new object[] { "stairs_3", Vector2.Zero, Values.LayerBottom, null }));

            ObjectTemplates.Add("break_sprite_end", null);

            ObjectTemplates.Add("wave1", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_0", 8, 125, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("wave2", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_2", 8, 125, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("wave6", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "wave_6", 8, 125, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("pondWoods", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_1", 8, 125, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("water1", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_3", 8, 125, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("wave3", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "wave_3", 8, 125, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("wave4", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "wave_4", 8, 125, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("wave5", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "wave_5", 8, 125, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("water2", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_4", 8, 150, true, 0, Values.LayerBottom })); // cant set to layer background because of the waterfall
            ObjectTemplates.Add("water3", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_5", 8, 150, true, 0, Values.LayerBottom }));
            // used in dungeon 4 and the boss needs to be on top but not on the same layer as the player; so we need to put the water on the background or add a new layer
            ObjectTemplates.Add("water4", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_6", 8, 150, true, 0, Values.LayerBackground }));
            ObjectTemplates.Add("water5", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_7", 8, 150, true, 0, Values.LayerBackground }));
            ObjectTemplates.Add("waterFall", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_8", 4, 100, true, 0, Values.LayerBottom })); // TOOD: does the raft waterfall move faster?
            ObjectTemplates.Add("waterLeft", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_left", 4, 100, true, 0, Values.LayerBottom })); // in the game they take 5-6 frames
            ObjectTemplates.Add("waterUp", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_up", 4, 100, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("waterRight", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_right", 4, 100, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("waterDown", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "water_down", 4, 100, true, 0, Values.LayerBottom }));

            ObjectTemplates.Add("flower", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "flower_0", 4, 120, false, 0, Values.LayerBottom }));
            ObjectTemplates.Add("flowerforest", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "flower_1", 4, 250, false, 0, Values.LayerBottom }));
            ObjectTemplates.Add("flowerforest2", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "flower_2", 4, 250, false, 0, Values.LayerBottom }));
            ObjectTemplates.Add("flower2", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "flower_3", 4, 250, false, 0, Values.LayerBottom }));
            ObjectTemplates.Add("flower3", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "flower_4", 4, 120, false, 0, Values.LayerBottom }));

            ObjectTemplates.Add("sand1", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "sand_0", 4, 175, true, 0, Values.LayerBackground }));
            ObjectTemplates.Add("sand2", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "sand_1", 4, 175, true, 0, Values.LayerBackground }));
            ObjectTemplates.Add("sand3", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "sand_2", 4, 175, true, 0, Values.LayerBackground }));

            ObjectTemplates.Add("2dPondWater", new GameObjectTemplate(typeof(ObjAnimatedShiftedTile), new object[] { Resources.SourceRectangle("water_2d_0"), -2, 0, 366, 0 }));
            ObjectTemplates.Add("2dWaterDungeon", new GameObjectTemplate(typeof(ObjAnimatedShiftedTile), new object[] { Resources.SourceRectangle("water_2d_1"), -2, 0, 366, 0 }));
            ObjectTemplates.Add("2dWaterDungeonDark", new GameObjectTemplate(typeof(ObjAnimatedShiftedTile), new object[] { Resources.SourceRectangle("water_2d_2"), -2, 0, 350, 0 }));
            ObjectTemplates.Add("2dWater", new GameObjectTemplate(typeof(ObjAnimatedShiftedTile), new object[] { Resources.SourceRectangle("water_2d_3"), -2, 0, 366, 0 }));
            ObjectTemplates.Add("2dWaterDungeon2", new GameObjectTemplate(typeof(ObjAnimatedShiftedTile), new object[] { Resources.SourceRectangle("water_2d_4"), -2, 0, 366, 0 }));

            ObjectTemplates.Add("colorTileRed", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "color_tile_red", 4, 200, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("colorTileGreen", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "color_tile_green", 4, 200, true, 0, Values.LayerBottom }));
            ObjectTemplates.Add("colorTileBlue", new GameObjectTemplate(typeof(ObjAnimatedTile), new object[] { "color_tile_blue", 4, 200, true, 0, Values.LayerBottom }));

            ObjectTemplates.Add("break_tile_end", null);

            ObjectTemplates.Add("tower_background", new GameObjectTemplate(typeof(ObjTowerBackground), new object[] { }));

            ObjectTemplates.Add("final_stairs", new GameObjectTemplate(typeof(ObjFinalStairs), new object[] { null }));
            ObjectTemplates.Add("final_background", new GameObjectTemplate(typeof(ObjFinalBackground), new object[] { null }));
            ObjectTemplates.Add("final_windfish", new GameObjectTemplate(typeof(ObjWindfish), new object[] { null }));
            ObjectTemplates.Add("final_fountain", new GameObjectTemplate(typeof(ObjFinalFountain), new object[] { null }));
            ObjectTemplates.Add("final_background_stairs", new GameObjectTemplate(typeof(ObjFinalBackgroundStairs), new object[] { null }));

            // fence
            ObjectTemplates.Add("fence", new GameObjectTemplate(typeof(ObjFence), new object[] { 15 }));
            ObjectTemplates.Add("fenceUL", new GameObjectTemplate(typeof(ObjFence), new object[] { 14 }));
            ObjectTemplates.Add("fenceU", new GameObjectTemplate(typeof(ObjFence), new object[] { 12 }));
            ObjectTemplates.Add("fenceUR", new GameObjectTemplate(typeof(ObjFence), new object[] { 13 }));
            ObjectTemplates.Add("fenceL", new GameObjectTemplate(typeof(ObjFence), new object[] { 10 }));
            ObjectTemplates.Add("fenceR", new GameObjectTemplate(typeof(ObjFence), new object[] { 5 }));
            ObjectTemplates.Add("fenceDL", new GameObjectTemplate(typeof(ObjFence), new object[] { 11 }));
            ObjectTemplates.Add("fenceD", new GameObjectTemplate(typeof(ObjFence), new object[] { 3 }));
            ObjectTemplates.Add("fenceDR", new GameObjectTemplate(typeof(ObjFence), new object[] { 7 }));
            ObjectTemplates.Add("fenceTR", new GameObjectTemplate(typeof(ObjFence), new object[] { 4 }));
            ObjectTemplates.Add("fenceTL", new GameObjectTemplate(typeof(ObjFence), new object[] { 8 }));
            ObjectTemplates.Add("fenceBR", new GameObjectTemplate(typeof(ObjFence), new object[] { 1 }));
            ObjectTemplates.Add("fenceBL", new GameObjectTemplate(typeof(ObjFence), new object[] { 2 }));

            ObjectTemplates.Add("break_fence_end", null);

            ObjectTemplates.Add("overworldObject", new GameObjectTemplate(typeof(ObjOverworld), new object[] { }));

            ObjectTemplates.Add("door", new GameObjectTemplate(typeof(ObjDoor), new object[] { 16, 16, null, null, null, 0, 0, true }));
            ObjectTemplates.Add("door2d", new GameObjectTemplate(typeof(ObjDoor2d), new object[] { 16, 16, null, null, null }));
            ObjectTemplates.Add("doorEgg", new GameObjectTemplate(typeof(ObjDoorEgg), new object[] { null }));

            ObjectTemplates.Add("lowFloor", new GameObjectTemplate(typeof(ObjFloor), new object[] { -2 }));
            ObjectTemplates.Add("water", new GameObjectTemplate(typeof(ObjWater), new object[] { -2 }));
            ObjectTemplates.Add("waterDeep", new GameObjectTemplate(typeof(ObjWaterDeep), new object[] { }));

            ObjectTemplates.Add("eggTeleporter", new GameObjectTemplate(typeof(ObjEggTeleporter), new object[] { }));
            ObjectTemplates.Add("stairs", new GameObjectTemplate(typeof(ObjSlow), new object[] { 0.5f }));
            ObjectTemplates.Add("teleporter", new GameObjectTemplate(typeof(ObjRaccoonTeleporter), new object[] { 0, 0, 16, 16, 0 }));
            ObjectTemplates.Add("jump", new GameObjectTemplate(typeof(ObjJump), new object[] { 0, 0, 16, 16, 1.0f, 1.0f, 0, false, false }));
            ObjectTemplates.Add("jumpRaft", new GameObjectTemplate(typeof(ObjJumpRaft), new object[] { 16, 80 }));

            ObjectTemplates.Add("objectSpawner", new GameObjectTemplate(typeof(ObjObjectSpawner), new object[] { null, null, null, null, true }));
            ObjectTemplates.Add("objectRespawner", new GameObjectTemplate(typeof(ObjObjectRespawner), new object[] { null, null, null }));
            ObjectTemplates.Add("positionDialog", new GameObjectTemplate(typeof(ObjPositionDialog), new object[] { null, null, null }));

            ObjectTemplates.Add("keysetter", new GameObjectTemplate(typeof(ObjKeySetter), new object[] { null, null }));
            ObjectTemplates.Add("keyConditionSetter", new GameObjectTemplate(typeof(ObjKeyConditionSetter), new object[] { null, null, true }));

            ObjectTemplates.Add("button", new GameObjectTemplate(typeof(ObjButton), new object[] { null }));
            ObjectTemplates.Add("leaveButton", new GameObjectTemplate(typeof(ObjButtonLeave), new object[] { null, 0, 16, 16, false }));
            ObjectTemplates.Add("buttonTouch", new GameObjectTemplate(typeof(ObjButtonTouch), new object[] { 16, 16, null, null, true, false }));
            ObjectTemplates.Add("buttonOrder", new GameObjectTemplate(typeof(ObjButtonOrder), new object[] { 0, null, null, false }));

            ObjectTemplates.Add("scriptBox", new GameObjectTemplate(typeof(ObjIntroStarter), new object[] { }));
            ObjectTemplates.Add("dialogBox", new GameObjectTemplate(typeof(ObjDialogBox), new object[] { null }));
            ObjectTemplates.Add("scriptOnTouch", new GameObjectTemplate(typeof(ObjScriptOnTouch), new object[] { 16, 16, null }));

            ObjectTemplates.Add("itemDisabler", new GameObjectTemplate(typeof(ObjItemDisabler), new object[] { }));
            ObjectTemplates.Add("shadowDisabler", new GameObjectTemplate(typeof(ObjShadowDisabler), new object[] { }));
            ObjectTemplates.Add("shadowSetter", new GameObjectTemplate(typeof(ObjShadowSetter), new object[] { 0.75f, 0.125f }));

            ObjectTemplates.Add("candyGrabber", new GameObjectTemplate(typeof(ObjCandyGrabber), new object[] { }));
            ObjectTemplates.Add("candyGrabberControls", new GameObjectTemplate(typeof(ObjCandyGrabberControls), new object[] { }));

            ObjectTemplates.Add("shellHouse", new GameObjectTemplate(typeof(ObjShellHouse), new object[] { }));
            ObjectTemplates.Add("swordSpawner", new GameObjectTemplate(typeof(ObjSwordSpawner), new object[] { }));

            ObjectTemplates.Add("waterCurrent0", new GameObjectTemplate(typeof(ObjQuicksand), new object[] { -0.5f, 0.0f, 1 }));
            ObjectTemplates.Add("waterCurrent1", new GameObjectTemplate(typeof(ObjQuicksand), new object[] { 0.0f, -0.5f, 1 }));
            ObjectTemplates.Add("waterCurrent2", new GameObjectTemplate(typeof(ObjQuicksand), new object[] { 0.5f, 0.0f, 1 }));
            ObjectTemplates.Add("waterCurrent3", new GameObjectTemplate(typeof(ObjQuicksand), new object[] { 0.0f, 0.5f, 1 }));

            ObjectTemplates.Add("waterCurrentFast0", new GameObjectTemplate(typeof(ObjQuicksand), new object[] { -0.75f, 0.0f, 1 }));
            ObjectTemplates.Add("waterCurrentFast1", new GameObjectTemplate(typeof(ObjQuicksand), new object[] { 0.0f, -0.75f, 1 }));
            ObjectTemplates.Add("waterCurrentFast2", new GameObjectTemplate(typeof(ObjQuicksand), new object[] { 0.75f, 0.0f, 1 }));
            ObjectTemplates.Add("waterCurrentFast3", new GameObjectTemplate(typeof(ObjQuicksand), new object[] { 0.0f, 0.75f, 1 }));

            ObjectTemplates.Add("quicksand", new GameObjectTemplate(typeof(ObjQuicksand), new object[] { 0.0f, 0.0f, 0 }));
            ObjectTemplates.Add("rollband", new GameObjectTemplate(typeof(ObjRollBand), new object[] { 0 }));
            ObjectTemplates.Add("rollbandEdge", new GameObjectTemplate(typeof(ObjRollBandEdge), new object[] { }));

            ObjectTemplates.Add("animatorL", new GameObjectTemplate(typeof(ObjAnimator), new object[] { 0, null, null, false }));

            ObjectTemplates.Add("fog", new GameObjectTemplate(typeof(ObjFog), new object[] { 1.0f, 0.4f }));

            ObjectTemplates.Add("break_real_stuff_start", null);

            ObjectTemplates.Add("destroyableStone", new GameObjectTemplate(typeof(ObjDestroyableStone), new object[] { Resources.SourceRectangle("destroyableStone"), null }));

            ObjectTemplates.Add("weatherBird", new GameObjectTemplate(typeof(ObjWeatherBird), new object[] { null }));

            ObjectTemplates.Add("chest", new GameObjectTemplate(typeof(ObjChest), new object[] { null, null, null, 0, false }));
            ObjectTemplates.Add("item", new GameObjectTemplate(typeof(ObjItem), new object[] { "", "", "", "", false }));
            ObjectTemplates.Add("itemTester", new GameObjectTemplate(typeof(ObjItemTester), new object[] { 16 }));
            ObjectTemplates.Add("storeItem", new GameObjectTemplate(typeof(ObjStoreItem), new object[] { null, 0, 1 }));

            ObjectTemplates.Add("signpost", new GameObjectTemplate(typeof(ObjSignpost), new object[] { null, "signpost_0", new Rectangle(0, 4, 16, 12), 1 }));
            ObjectTemplates.Add("signpostWoods", new GameObjectTemplate(typeof(ObjSignpost), new object[] { null, "signpost_1", new Rectangle(0, 4, 16, 12), 1 }));
            ObjectTemplates.Add("sign", new GameObjectTemplate(typeof(ObjSignpost), new object[] { null, null, new Rectangle(0, 0, 16, 16), -1 }));
            ObjectTemplates.Add("pushDialog", new GameObjectTemplate(typeof(ObjOnPushDialog), new object[] { null, 16, 16 }));
            ObjectTemplates.Add("pushKeySetter", new GameObjectTemplate(typeof(ObjOnPushKeySetter), new object[] { null, 75, true }));
            ObjectTemplates.Add("hitKeySetter", new GameObjectTemplate(typeof(ObjOnHitKeySetter), new object[] { null, 0, true, 16, 16 }));
            ObjectTemplates.Add("shellHitSpawner", new GameObjectTemplate(typeof(ObjOnDashSpawner), new object[] { null, "shell" }));
            ObjectTemplates.Add("sideWaves", new GameObjectTemplate(typeof(ObjIslandBackground), new object[] { }));

            ObjectTemplates.Add("aquaticPlant", new GameObjectTemplate(typeof(ObjAquaticPlant), new object[] { }));
            ObjectTemplates.Add("stoneSpawner", new GameObjectTemplate(typeof(ObjStoneSpawner), new object[] { }));

            ObjectTemplates.Add("bush", new GameObjectTemplate(typeof(ObjBush), new object[] { null, "bush_0", true, true, false, Values.LayerPlayer, null }));
            ObjectTemplates.Add("bushForest", new GameObjectTemplate(typeof(ObjBush), new object[] { null, "bush_1", true, true, false, Values.LayerPlayer, null }));
            ObjectTemplates.Add("gras", new GameObjectTemplate(typeof(ObjBush), new object[] { null, "grass_0", false, false, true, Values.LayerBottom, null }));
            ObjectTemplates.Add("gras0", new GameObjectTemplate(typeof(ObjBush), new object[] { null, "grass_0_0", false, false, true, Values.LayerBottom, null }));
            ObjectTemplates.Add("gras1", new GameObjectTemplate(typeof(ObjBush), new object[] { null, "grass_0_1", false, false, true, Values.LayerBottom, null }));
            ObjectTemplates.Add("gras2", new GameObjectTemplate(typeof(ObjBush), new object[] { null, "grass_0_2", false, false, true, Values.LayerBottom, null }));
            ObjectTemplates.Add("gras3", new GameObjectTemplate(typeof(ObjBush), new object[] { null, "grass_0_3", false, false, true, Values.LayerBottom, null }));
            ObjectTemplates.Add("grasForest", new GameObjectTemplate(typeof(ObjBush), new object[] { null, "grass_1", false, false, true, Values.LayerBottom, null }));
            ObjectTemplates.Add("grasSwamp", new GameObjectTemplate(typeof(ObjBush), new object[] { null, "grass_2", false, false, true, Values.LayerBottom, null }));

            ObjectTemplates.Add("gravestone", new GameObjectTemplate(typeof(ObjMoveStone), new object[] { 15, null, "gravestone", new Rectangle(0, -12, 16, 12), Values.LayerPlayer, 1, true, null }));
            ObjectTemplates.Add("moveStone", new GameObjectTemplate(typeof(ObjMoveStone), new object[] { 15, null, "movestone_0", new Rectangle(0, -16, 16, 16), Values.LayerBottom, 0, false, null }));
            ObjectTemplates.Add("moveStoneCave", new GameObjectTemplate(typeof(ObjMoveStone), new object[] { 15, null, "movestone_1", new Rectangle(0, -16, 16, 16), Values.LayerBottom, 0, false, null }));
            ObjectTemplates.Add("moveStoneFrogHouse", new GameObjectTemplate(typeof(ObjMoveStone), new object[] { 15, null, "movestone_2", new Rectangle(0, -16, 16, 16), Values.LayerBottom, 0, false, null }));
            // why was the height 14???
            ObjectTemplates.Add("moveStoneD3", new GameObjectTemplate(typeof(ObjMoveStone), new object[] { 15, null, "movestone_3", new Rectangle(0, -16, 16, 16), Values.LayerBottom, 0, false, null }));

            ObjectTemplates.Add("leverStone", new GameObjectTemplate(typeof(ObjLeverStone), new object[] { 0 }));

            ObjectTemplates.Add("stone", new GameObjectTemplate(typeof(ObjStone), new object[] { "stone_0", null, null, "stone", false, false }));
            ObjectTemplates.Add("stoneWoods", new GameObjectTemplate(typeof(ObjStone), new object[] { "stone_1", null, null, "stone", false, false }));
            ObjectTemplates.Add("stoneSkull", new GameObjectTemplate(typeof(ObjStone), new object[] { "skull", null, null, null, false, false }));
            ObjectTemplates.Add("pot", new GameObjectTemplate(typeof(ObjStone), new object[] { "pot_0", null, null, "stone", false, true }));
            ObjectTemplates.Add("pot2", new GameObjectTemplate(typeof(ObjStone), new object[] { "pot_1", null, null, "stone", false, true }));
            ObjectTemplates.Add("pot2D", new GameObjectTemplate(typeof(ObjStone), new object[] { "pot_2", null, null, null, false, false }));
            ObjectTemplates.Add("d6Statue", new GameObjectTemplate(typeof(ObjStone), new object[] { "d6_statue", null, null, null, true, false }));

            ObjectTemplates.Add("castleDoor", new GameObjectTemplate(typeof(ObjCastleDoor), new object[] { null }));

            ObjectTemplates.Add("cactus", new GameObjectTemplate(typeof(ObjCactus), new object[] { }));

            ObjectTemplates.Add("overworldTeleporter", new GameObjectTemplate(typeof(ObjOverworldTeleporter), new object[] { -1 }));

            ObjectTemplates.Add("bridge", new GameObjectTemplate(typeof(ObjBridge), new object[] { }));
            ObjectTemplates.Add("pullBridge", new GameObjectTemplate(typeof(ObjPullBridge), new object[] { null, false }));

            ObjectTemplates.Add("waterFallSpawner", new GameObjectTemplate(typeof(ObjWaterfall), new object[] { null }));

            ObjectTemplates.Add("book", new GameObjectTemplate(typeof(ObjBook), new object[] { null, null, 0 }));
            ObjectTemplates.Add("bed", new GameObjectTemplate(typeof(ObjBed), new object[] { null, null }));
            ObjectTemplates.Add("raft", new GameObjectTemplate(typeof(ObjRaft), new object[] { null }));

            // dungeon start
            ObjectTemplates.Add("break_dungeon_start", null);

            ObjectTemplates.Add("dungeon", new GameObjectTemplate(typeof(ObjDungeon), new object[] { null, true, 0 }));

            ObjectTemplates.Add("upperLevel", new GameObjectTemplate(typeof(ObjUpperLevel), new object[] { 1 }));
            ObjectTemplates.Add("upperLevel2", new GameObjectTemplate(typeof(ObjUpperLevel), new object[] { 2 }));

            ObjectTemplates.Add("dungeonBlackRoom", new GameObjectTemplate(typeof(ObjDungeonBlackRoom), new object[] { null, 160, 128 }));
            ObjectTemplates.Add("roomDarkener", new GameObjectTemplate(typeof(ObjRoomDarkener), new object[] { 0.8f, 0.3f }));
            ObjectTemplates.Add("colorShift", new GameObjectTemplate(typeof(ObjColorShift), new object[] { 0, 16, 16 }));
            ObjectTemplates.Add("dungeonKeyhole", new GameObjectTemplate(typeof(ObjKeyhole), new object[] { null, null, null }));
            ObjectTemplates.Add("enemytrigger", new GameObjectTemplate(typeof(ObjEnemyTrigger), new object[] { null }));
            ObjectTemplates.Add("hitTrigger", new GameObjectTemplate(typeof(ObjHitTrigger), new object[] { 0, null, 16, 16, 200, true, true }));
            ObjectTemplates.Add("killOrderTrigger", new GameObjectTemplate(typeof(ObjKillTrigger), new object[] { null }));
            ObjectTemplates.Add("graveTrigger", new GameObjectTemplate(typeof(ObjGraveTrigger), new object[] { null }));
            ObjectTemplates.Add("objectHider", new GameObjectTemplate(typeof(ObjObjectHider), new object[] { }));

            // cant change the name because we need it to be the same while adding it to the map
            ObjectTemplates.Add("link2dspawner", new GameObjectTemplate(typeof(Obj2DMode), new object[] { }));
            ObjectTemplates.Add("dungeonLadder", new GameObjectTemplate(typeof(ObjLadder), new object[] { false }));
            ObjectTemplates.Add("dungeonLadderTop", new GameObjectTemplate(typeof(ObjLadder), new object[] { true }));
            ObjectTemplates.Add("dungeonPullLever", new GameObjectTemplate(typeof(ObjPullLever), new object[] { 0.18f, null }));

            ObjectTemplates.Add("ddoor", new GameObjectTemplate(typeof(ObjDungeonDoor), new object[] { 0, null, 0, null }));
            ObjectTemplates.Add("dungeonEntrance", new GameObjectTemplate(typeof(ObjDungeonEntrance), new object[] { "dungeon_entrance", null }));
            ObjectTemplates.Add("dungeonSixEntry", new GameObjectTemplate(typeof(ObjDungeonSixEntry), new object[] { null }));
            ObjectTemplates.Add("dungeon7_tower", new GameObjectTemplate(typeof(ObjTower), new object[] { null }));
            ObjectTemplates.Add("mermaid_statue", new GameObjectTemplate(typeof(ObjMermaidStatue), new object[] { null }));

            ObjectTemplates.Add("destroyable_barrier", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("destroyable_barrier"), "", 0, false, "cracked_rock" }));
            ObjectTemplates.Add("stoneWall", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_0"), "", 0, true, null }));
            ObjectTemplates.Add("destroyableWallCave", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_1"), "", 0, true, null }));
            ObjectTemplates.Add("destroyableWallColorDungeon", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_7"), "", 0, true, null }));
            ObjectTemplates.Add("destroyableWallDungeon7", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_9"), "", 0, true, null }));

            ObjectTemplates.Add("dungeonCrystal", new GameObjectTemplate(typeof(ObjCrystal), new object[] { "crystal_2", 0, false, null }));
            ObjectTemplates.Add("caveCrystal", new GameObjectTemplate(typeof(ObjCrystal), new object[] { "crystal_0", 1, false, null }));
            ObjectTemplates.Add("crystalD4", new GameObjectTemplate(typeof(ObjCrystal), new object[] { "crystal_1", 1, false, null }));
            ObjectTemplates.Add("hardCrystal", new GameObjectTemplate(typeof(ObjCrystal), new object[] { "crystal_hard", 1, true, "crystal_hard" }));

            ObjectTemplates.Add("caveBreakingFloor", new GameObjectTemplate(typeof(ObjBreakingFloor), new object[] { "breaking_floor_0" }));
            ObjectTemplates.Add("caveBreakingFloor2", new GameObjectTemplate(typeof(ObjBreakingFloor), new object[] { "breaking_floor_1" }));
            ObjectTemplates.Add("caveBreakingFloor3", new GameObjectTemplate(typeof(ObjBreakingFloor), new object[] { "breaking_floor_2" }));
            ObjectTemplates.Add("dungeonHole", new GameObjectTemplate(typeof(ObjBreakingFloor), new object[] { "breaking_floor_3" }));
            ObjectTemplates.Add("breakingFloorCastle", new GameObjectTemplate(typeof(ObjBreakingFloor), new object[] { "breaking_floor_4" }));
            ObjectTemplates.Add("dungeon5BreakingFloor", new GameObjectTemplate(typeof(ObjBreakingFloor), new object[] { "breaking_floor_5" }));
            ObjectTemplates.Add("dungeon2BreakingFloor", new GameObjectTemplate(typeof(ObjBreakingFloor), new object[] { "breaking_floor_6" }));
            ObjectTemplates.Add("dungeon8BreakingFloor", new GameObjectTemplate(typeof(ObjBreakingFloor), new object[] { "breaking_floor_7" }));
            ObjectTemplates.Add("breakingFloorHouse", new GameObjectTemplate(typeof(ObjBreakingFloor), new object[] { "breaking_floor_8" }));

            ObjectTemplates.Add("dungeonBlacker", new GameObjectTemplate(typeof(ObjDungeonBlacker), new object[] { 255, 255, 255, 200 }));
            ObjectTemplates.Add("houseBlacker", new GameObjectTemplate(typeof(ObjDungeonBlacker), new object[] { 255, 220, 180, 175 }));
            ObjectTemplates.Add("caveBlacker", new GameObjectTemplate(typeof(ObjDungeonBlacker), new object[] { 255, 230, 200, 125 }));

            ObjectTemplates.Add("music", new GameObjectTemplate(typeof(ObjMusic), new object[] { null }));
            ObjectTemplates.Add("musicTiles", new GameObjectTemplate(typeof(ObjMusicTile), new object[] { }));
            ObjectTemplates.Add("compassSound", new GameObjectTemplate(typeof(ObjCompassSound), new object[] { null }));
            ObjectTemplates.Add("shoreSound", new GameObjectTemplate(typeof(ObjShoreSound), new object[] { }));
            ObjectTemplates.Add("waterfallSound", new GameObjectTemplate(typeof(ObjWaterfallSound), new object[] { }));

            ObjectTemplates.Add("dungeonFairy", new GameObjectTemplate(typeof(ObjDungeonFairy), new object[] { 0, null }));
            ObjectTemplates.Add("dungeonBall", new GameObjectTemplate(typeof(ObjBall), new object[] { null }));
            ObjectTemplates.Add("dungeonPillar", new GameObjectTemplate(typeof(ObjDungeonPillar), new object[] { null }));

            ObjectTemplates.Add("lava", new GameObjectTemplate(typeof(ObjLava), new object[] { }));
            ObjectTemplates.Add("lava2d", new GameObjectTemplate(typeof(ObjLavaField), new object[] { "lava_2d", 4, 175, true, 0, Values.LayerBackground }));

            // real stuff
            ObjectTemplates.Add("break_dungeon_start_real", null);

            ObjectTemplates.Add("light", new GameObjectTemplate(typeof(ObjLight), new object[] { 128, 255, 255, 255, 255, 0 }));
            //ObjectTemplates.Add("lightHouse", new GameObjectTemplate(typeof(ObjLight), new object[] { 128, 255, 255, 255, 100, 0 }));
            ObjectTemplates.Add("caveLight", new GameObjectTemplate(typeof(ObjLight), new object[] { 128, 255, 255, 255, 100, 0 }));
            ObjectTemplates.Add("doorLight", new GameObjectTemplate(typeof(ObjLight), new object[] { 128, 255, 255, 255, 100, 0 }));
            ObjectTemplates.Add("dungeon2dLight", new GameObjectTemplate(typeof(ObjLight), new object[] { 96, 255, 200, 200, 200, 0 }));
            ObjectTemplates.Add("spriteLight", new GameObjectTemplate(typeof(ObjLightSprite), new object[] { null, 255, 255, 255, 100, 0, 0 }));

            ObjectTemplates.Add("lamp", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_floor", 0, true, false, null }));
            ObjectTemplates.Add("lamp2", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_torch", 0, false, false, null }));
            ObjectTemplates.Add("torch_d2_2d", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_torch_blue", 0, false, false, null }));
            ObjectTemplates.Add("torch_d4_2d", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/torch_d4_d4", 0, false, false, null }));
            ObjectTemplates.Add("torch_d6_2d", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/torch_d6", 0, false, false, null }));

            ObjectTemplates.Add("lamp_wall_0", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall", 0, false, false, null }));
            ObjectTemplates.Add("lamp_wall_1", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall", 1, false, false, null }));
            ObjectTemplates.Add("lamp_wall_2", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall", 2, false, false, null }));
            ObjectTemplates.Add("lamp_wall_3", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall", 3, false, false, null }));

            ObjectTemplates.Add("lamp_wall_house_0", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall_1", 0, false, false, null }));
            ObjectTemplates.Add("lamp_wall_house_1", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall_1", 1, false, false, null }));
            ObjectTemplates.Add("lamp_wall_house_2", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall_1", 2, false, false, null }));
            ObjectTemplates.Add("lamp_wall_house_3", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall_1", 3, false, false, null }));

            ObjectTemplates.Add("lamp_wall_dt_0", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall_2", 0, false, false, null }));
            ObjectTemplates.Add("lamp_wall_dt_1", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall_2", 1, false, false, null }));
            ObjectTemplates.Add("lamp_wall_dt_2", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall_2", 2, false, false, null }));
            ObjectTemplates.Add("lamp_wall_dt_3", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/lamp_wall_2", 3, false, false, null }));

            ObjectTemplates.Add("torch_d4", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/torch_d4", 0, false, false, null }));
            ObjectTemplates.Add("torch_d8", new GameObjectTemplate(typeof(ObjLamp), new object[] { "Objects/torch_d8", 0, false, false, null }));

            ObjectTemplates.Add("dungeonWall", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_2"), "", 0, true, null }));
            ObjectTemplates.Add("dungeonWall3", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_3"), "", 0, true, null }));
            ObjectTemplates.Add("dungeonWall3cracks", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_4"), "", 0, true, null }));
            ObjectTemplates.Add("dungeon4Block", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_5"), "", 0, false, "rock_cracks" }));
            ObjectTemplates.Add("dungeon6Wall", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_6"), "", 0, true, null }));
            ObjectTemplates.Add("dungeon7Wall", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_8"), "", 0, true, null }));
            ObjectTemplates.Add("dungeon8Wall", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_10"), "", 0, true, null }));
            ObjectTemplates.Add("dungeon8WallCracks", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_11"), "", 0, true, null }));
            ObjectTemplates.Add("caveWallBottom", new GameObjectTemplate(typeof(ObjDestroyableBarrier), new object[] { Resources.SourceRectangle("stone_wall_12"), "", 0, true, null }));

            ObjectTemplates.Add("dungeonSwitch", new GameObjectTemplate(typeof(ObjDungeonSwitch), new object[] { null }));
            ObjectTemplates.Add("dungeonOneWay", new GameObjectTemplate(typeof(ObjDungeonOneWay), new object[] { }));
            ObjectTemplates.Add("dungeonTeleporter", new GameObjectTemplate(typeof(ObjDungeonTeleporter), new object[] { null, null }));
            ObjectTemplates.Add("keyholeBlock", new GameObjectTemplate(typeof(ObjKeyholeBlock), new object[] { null }));

            ObjectTemplates.Add("dungeonBarriere", new GameObjectTemplate(typeof(ObjDungeonBarrier), new object[] { null, false, 0 }));
            ObjectTemplates.Add("dungeonBarriereOrange", new GameObjectTemplate(typeof(ObjDungeonBarrier), new object[] { null, false, 1 }));
            ObjectTemplates.Add("dungeonBarriereRed", new GameObjectTemplate(typeof(ObjDungeonBarrier), new object[] { null, false, 2 }));

            ObjectTemplates.Add("dungeonColorSwitch", new GameObjectTemplate(typeof(ObjDungeonColorSwitch), new object[] { null, 0, 2, 0, 0 }));

            ObjectTemplates.Add("break_dungeon_end", null);

            ObjectTemplates.Add("hole", new GameObjectTemplate(typeof(ObjHole), new object[] { 14, 14, Rectangle.Empty, 1, 1, 0 }));
            ObjectTemplates.Add("visiblehole", new GameObjectTemplate(typeof(ObjHole), new object[] { 14, 14, Resources.SourceRectangle("hole_0"), 1, 1, 0 }));
            ObjectTemplates.Add("fullHole", new GameObjectTemplate(typeof(ObjHole), new object[] { 16, 16, Rectangle.Empty, 0, 0, 0 }));
            ObjectTemplates.Add("holeReset", new GameObjectTemplate(typeof(ObjHoleResetPoint), new object[] { 0 }));
            ObjectTemplates.Add("holeTeleporter", new GameObjectTemplate(typeof(ObjHoleTeleporter), new object[] { null, null }));

            ObjectTemplates.Add("doorEnding", new GameObjectTemplate(typeof(ObjDoorEnding), new object[] { }));

            ObjectTemplates.Add("boat", new GameObjectTemplate(typeof(ObjBoat), new object[] { }));
            ObjectTemplates.Add("movingPlatform2D", new GameObjectTemplate(typeof(ObjMovingPlatform), new object[] { 0, 0, 0.0f, 1000, 0 }));
            ObjectTemplates.Add("chainPlatform", new GameObjectTemplate(typeof(ObjChainPlatform), new object[] { null, 0, 0 }));
            ObjectTemplates.Add("spikes2D", new GameObjectTemplate(typeof(ObjSpikes2D), new object[] { }));

            ObjectTemplates.Add("dungeonRollBand", new GameObjectTemplate(typeof(ObjRollBandDungeon), new object[] { 0 }));
            ObjectTemplates.Add("dungeonOwl", new GameObjectTemplate(typeof(ObjDungeonOwl), new object[] { null }));
            ObjectTemplates.Add("dungeonHorseHead", new GameObjectTemplate(typeof(ObjDungeonHorseHead), new object[] { null, 0 }));
            ObjectTemplates.Add("colorJumpTile", new GameObjectTemplate(typeof(ObjColorJumpTile), new object[] { 0 }));
            ObjectTemplates.Add("spikes", new GameObjectTemplate(typeof(ObjSpikes), new object[] { }));
            ObjectTemplates.Add("iceBlock", new GameObjectTemplate(typeof(ObjIceBlock), new object[] { }));

            ObjectTemplates.Add("break_hole_end", null);
            ObjectTemplates.Add("break_people_start", null);

            // npcs
            ObjectTemplates.Add("person", new GameObjectTemplate(typeof(ObjPerson), new object[] { null, new Rectangle(0, 0, 14, 10), Vector2.Zero, null }));
            ObjectTemplates.Add("personNew", new GameObjectTemplate(typeof(ObjPersonNew), new object[] { null, null, null, null, new Rectangle(0, 0, 14, 10) }));
            ObjectTemplates.Add("letterBoy", new GameObjectTemplate(typeof(ObjLetterBoy), new object[] { null }));
            ObjectTemplates.Add("maria", new GameObjectTemplate(typeof(ObjMarin), new object[] { }));
            ObjectTemplates.Add("mariaDisabler", new GameObjectTemplate(typeof(ObjMarinDisabler), new object[] { }));
            ObjectTemplates.Add("grandmother", new GameObjectTemplate(typeof(ObjGrandmother), new object[] { null, null }));
            ObjectTemplates.Add("mariaDungeonEntry", new GameObjectTemplate(typeof(ObjMarinDungeonEntry), new object[] { 16, 16 }));
            ObjectTemplates.Add("owl", new GameObjectTemplate(typeof(ObjOwl), new object[] { null, new Rectangle(-16, 32, 48, 32), false, "owl", 0 }));
            ObjectTemplates.Add("fisherman", new GameObjectTemplate(typeof(ObjFisherman), new object[] { }));
            ObjectTemplates.Add("alligator", new GameObjectTemplate(typeof(ObjAlligator), new object[] { }));
            ObjectTemplates.Add("raccoon", new GameObjectTemplate(typeof(ObjRaccoon), new object[] { }));
            ObjectTemplates.Add("shopkeeper", new GameObjectTemplate(typeof(ObjShopkeeper), new object[] { }));
            ObjectTemplates.Add("tracy", new GameObjectTemplate(typeof(ObjTracy), new object[] { }));
            ObjectTemplates.Add("mamu", new GameObjectTemplate(typeof(ObjMamu), new object[] { null }));
            ObjectTemplates.Add("manbo", new GameObjectTemplate(typeof(ObjManbo), new object[] { null }));
            ObjectTemplates.Add("walrus", new GameObjectTemplate(typeof(ObjWalrus), new object[] { null }));
            ObjectTemplates.Add("walrusSwim", new GameObjectTemplate(typeof(ObjWalrusSwim), new object[] { null }));
            ObjectTemplates.Add("mermaid", new GameObjectTemplate(typeof(ObjMermaid), new object[] { null }));
            ObjectTemplates.Add("ghost", new GameObjectTemplate(typeof(ObjGhost), new object[] { }));
            ObjectTemplates.Add("lostBoy", new GameObjectTemplate(typeof(ObjLostBoy), new object[] { }));
            ObjectTemplates.Add("photoMouse", new GameObjectTemplate(typeof(ObjPhotoMouse), new object[] { null, null }));
            ObjectTemplates.Add("chickenDude", new GameObjectTemplate(typeof(ObjChickenDude), new object[] { null }));
            ObjectTemplates.Add("painter", new GameObjectTemplate(typeof(ObjPainter), new object[] { }));
            ObjectTemplates.Add("hippo", new GameObjectTemplate(typeof(ObjHippo), new object[] { }));
            ObjectTemplates.Add("trendy", new GameObjectTemplate(typeof(ObjTrendy), new object[] { }));

            ObjectTemplates.Add("BowWow", new GameObjectTemplate(typeof(ObjBowWow), new object[] { null }));
            ObjectTemplates.Add("npcMonkey", new GameObjectTemplate(typeof(ObjMonkey), new object[] { }));
            ObjectTemplates.Add("bobWowSmall", new GameObjectTemplate(typeof(ObjBowWowSmall), new object[] { null }));
            ObjectTemplates.Add("bird", new GameObjectTemplate(typeof(ObjBird), new object[] { }));
            ObjectTemplates.Add("cock", new GameObjectTemplate(typeof(ObjCock), new object[] { null }));
            ObjectTemplates.Add("letterBird", new GameObjectTemplate(typeof(ObjLetterBird), new object[] { "NPCs/letterBird" }));
            ObjectTemplates.Add("letterBirdGreen", new GameObjectTemplate(typeof(ObjLetterBird), new object[] { "NPCs/letterBirdGreen" }));
            ObjectTemplates.Add("dogo", new GameObjectTemplate(typeof(ObjDog), new object[] { }));
            ObjectTemplates.Add("mouse", new GameObjectTemplate(typeof(ObjMouse), new object[] { }));
            ObjectTemplates.Add("frog", new GameObjectTemplate(typeof(ObjFrog), new object[] { }));
            ObjectTemplates.Add("butterfly", new GameObjectTemplate(typeof(ObjButterfly), new object[] { }));
            ObjectTemplates.Add("fairy", new GameObjectTemplate(typeof(ObjFairy), new object[] { null }));
            ObjectTemplates.Add("npcBat", new GameObjectTemplate(typeof(ObjBat), new object[] { null }));
            ObjectTemplates.Add("npcColorDungeon", new GameObjectTemplate(typeof(ObjColorDungeonNPC), new object[] { null, false }));

            ObjectTemplates.Add("honeycomb", new GameObjectTemplate(typeof(ObjHoneycomb), new object[] { null }));
            ObjectTemplates.Add("tarinZZZ", new GameObjectTemplate(typeof(ObjZZZSpawner), new object[] { }));

            // fishing game
            ObjectTemplates.Add("fish_small", new GameObjectTemplate(typeof(ObjFish), new object[] { 0, 0, 0 }));
            ObjectTemplates.Add("fish_big", new GameObjectTemplate(typeof(ObjFish), new object[] { 1, 0, 0 }));
            ObjectTemplates.Add("fishing_link", new GameObjectTemplate(typeof(ObjLinkFishing), new object[] { }));
            ObjectTemplates.Add("ballGame", new GameObjectTemplate(typeof(ObjBallGame), new object[] { null }));
            ObjectTemplates.Add("ballChildrenAttacked", new GameObjectTemplate(typeof(ObjBallChildrenAttacked), new object[] { null }));

            ObjectTemplates.Add("break_people_end", null);
            ObjectTemplates.Add("break_enemies_start", null);

            // enemies
            ObjectTemplates.Add("enemy_respawner", new GameObjectTemplate(typeof(ObjEnemyRespawner), new object[] { null, null }));
            ObjectTemplates.Add("e1", new GameObjectTemplate(typeof(EnemySeaUrchin), new object[] { }));
            ObjectTemplates.Add("e2", new GameObjectTemplate(typeof(EnemyOctorok), new object[] { }));
            ObjectTemplates.Add("e_wingedOctorok", new GameObjectTemplate(typeof(EnemyOctorokWinged), new object[] { }));
            ObjectTemplates.Add("e3", new GameObjectTemplate(typeof(EnemyLeever), new object[] { }));
            ObjectTemplates.Add("e4", new GameObjectTemplate(typeof(EnemyCrab), new object[] { }));
            ObjectTemplates.Add("e5", new GameObjectTemplate(typeof(EnemyMoblin), new object[] { }));
            ObjectTemplates.Add("moblinSword", new GameObjectTemplate(typeof(EnemyMoblinSword), new object[] { }));
            ObjectTemplates.Add("shroudedStalfos", new GameObjectTemplate(typeof(EnemyShroudedStalfos), new object[] { }));
            ObjectTemplates.Add("stalfosKnight", new GameObjectTemplate(typeof(EnemyStalfosKnight), new object[] { }));
            ObjectTemplates.Add("e19", new GameObjectTemplate(typeof(EnemyMoblinPig), new object[] { }));
            ObjectTemplates.Add("e_moblinPigSword", new GameObjectTemplate(typeof(EnemyMoblinPigSword), new object[] { }));
            ObjectTemplates.Add("e_darknutSpear", new GameObjectTemplate(typeof(EnemyDarknutSpear), new object[] { }));
            ObjectTemplates.Add("e_darknut", new GameObjectTemplate(typeof(EnemyDarknut), new object[] { }));
            ObjectTemplates.Add("e_wallKnight", new GameObjectTemplate(typeof(ObjWallKnight), new object[] { false }));
            ObjectTemplates.Add("e_madBomber", new GameObjectTemplate(typeof(EnemyMadBomber), new object[] { }));
            ObjectTemplates.Add("e6", new GameObjectTemplate(typeof(EnemyBladeTrap), new object[] { 0, 0, 0, 0 }));
            ObjectTemplates.Add("e8", new GameObjectTemplate(typeof(EnemyHardhatBeetle), new object[] { }));
            ObjectTemplates.Add("e9", new GameObjectTemplate(typeof(EnemyGreenZol), new object[] { 0, false }));
            ObjectTemplates.Add("e15", new GameObjectTemplate(typeof(EnemyRedZol), new object[] { }));
            ObjectTemplates.Add("e7", new GameObjectTemplate(typeof(EnemyGel), new object[] { }));
            ObjectTemplates.Add("e10", new GameObjectTemplate(typeof(EnemyKeese), new object[] { }));
            ObjectTemplates.Add("e11", new GameObjectTemplate(typeof(EnemySpark), new object[] { 0, true, null }));
            ObjectTemplates.Add("e_antiFairy", new GameObjectTemplate(typeof(EnemyAntiFairy), new object[] { }));
            ObjectTemplates.Add("e12", new GameObjectTemplate(typeof(EnemyMiniMoldorm), new object[] { }));
            ObjectTemplates.Add("polsVoice", new GameObjectTemplate(typeof(EnemyPolsVoice), new object[] { }));
            ObjectTemplates.Add("e_dungeonGhost", new GameObjectTemplate(typeof(EnemyBooBuddy), new object[] { null }));
            ObjectTemplates.Add("e_bomber", new GameObjectTemplate(typeof(EnemyBomber), new object[] { }));
            ObjectTemplates.Add("e_pokey", new GameObjectTemplate(typeof(EnemyPokey), new object[] { }));
            ObjectTemplates.Add("e_spinyBeetle", new GameObjectTemplate(typeof(EnemySpinyBeetle), new object[] { 0 }));
            ObjectTemplates.Add("e_tektite", new GameObjectTemplate(typeof(EnemyTektite), new object[] { }));
            ObjectTemplates.Add("e13", new GameObjectTemplate(typeof(EnemyStalfosOrange), new object[] { false }));
            ObjectTemplates.Add("stalfosGreen", new GameObjectTemplate(typeof(EnemyStalfosGreen), new object[] { }));
            ObjectTemplates.Add("e_armos", new GameObjectTemplate(typeof(EnemyArmos), new object[] { false }));
            ObjectTemplates.Add("e_Vacuum", new GameObjectTemplate(typeof(EnemyVacuum), new object[] { null, null, false }));
            ObjectTemplates.Add("e_bombite", new GameObjectTemplate(typeof(EnemyBombite), new object[] { }));
            ObjectTemplates.Add("e_bombiteGreen", new GameObjectTemplate(typeof(EnemyBombiteGreen), new object[] { }));
            ObjectTemplates.Add("e16", new GameObjectTemplate(typeof(EnemyLikeLike), new object[] { }));
            ObjectTemplates.Add("e17", new GameObjectTemplate(typeof(EnemyBuzzBlob), new object[] { }));
            ObjectTemplates.Add("e18", new GameObjectTemplate(typeof(EnemyRiverZora), new object[] { }));
            ObjectTemplates.Add("e20", new GameObjectTemplate(typeof(EnemyCrow), new object[] { false }));
            ObjectTemplates.Add("e_raven", new GameObjectTemplate(typeof(EnemyRaven), new object[] { }));
            ObjectTemplates.Add("e21", new GameObjectTemplate(typeof(EnemyGhini), new object[] { false, false }));
            ObjectTemplates.Add("e_giantGhini", new GameObjectTemplate(typeof(EnemyGhiniGiant), new object[] { false }));
            ObjectTemplates.Add("zombie", new GameObjectTemplate(typeof(EnemyZombie), new object[] { }));
            ObjectTemplates.Add("zombieSpawner", new GameObjectTemplate(typeof(ObjZombieSpawner), new object[] { 1250 }));
            ObjectTemplates.Add("e23", new GameObjectTemplate(typeof(EnemyPincer), new object[] { }));
            ObjectTemplates.Add("e_Beetle", new GameObjectTemplate(typeof(EnemyBeetle), new object[] { }));
            ObjectTemplates.Add("e_BeetleSpawner", new GameObjectTemplate(typeof(ObjBeetleSpawner), new object[] { }));
            ObjectTemplates.Add("spikedBeetle", new GameObjectTemplate(typeof(EnemySpikedBeetle), new object[] { }));
            ObjectTemplates.Add("goponga_flower", new GameObjectTemplate(typeof(EnemyGopongaFlower), new object[] { }));
            ObjectTemplates.Add("goponga_flower_giant", new GameObjectTemplate(typeof(EnemyGopongaFlowerGiant), new object[] { }));
            ObjectTemplates.Add("e_fish", new GameObjectTemplate(typeof(EnemyFish), new object[] { }));
            ObjectTemplates.Add("cardboy", new GameObjectTemplate(typeof(EnemyCardBoy), new object[] { 0, null }));
            ObjectTemplates.Add("monkey", new GameObjectTemplate(typeof(EnemyMonkey), new object[] { }));
            ObjectTemplates.Add("torchTrap", new GameObjectTemplate(typeof(EnemyTorchTrap), new object[] { null }));
            ObjectTemplates.Add("e_pairodd", new GameObjectTemplate(typeof(EnemyPairodd), new object[] { }));
            ObjectTemplates.Add("maskMimic", new GameObjectTemplate(typeof(EnemyMaskMimic), new object[] { }));
            ObjectTemplates.Add("e_ArmMimic", new GameObjectTemplate(typeof(EnemyArmMimic), new object[] { }));
            ObjectTemplates.Add("e_waterTektite", new GameObjectTemplate(typeof(EnemyWaterTektite), new object[] { }));
            ObjectTemplates.Add("e_peahat", new GameObjectTemplate(typeof(EnemyPeahat), new object[] { }));
            ObjectTemplates.Add("e_ironMask", new GameObjectTemplate(typeof(EnemyIronMask), new object[] { }));
            ObjectTemplates.Add("e_star", new GameObjectTemplate(typeof(EnemyStar), new object[] { }));
            ObjectTemplates.Add("e_flyingTile", new GameObjectTemplate(typeof(EnemyFlyingTile), new object[] { null, 0, 0 }));
            ObjectTemplates.Add("e_wizzrobe", new GameObjectTemplate(typeof(EnemyWizzrobe), new object[] { }));
            ObjectTemplates.Add("e_beamos", new GameObjectTemplate(typeof(EnemyBeamos), new object[] { }));
            ObjectTemplates.Add("e_camoGoblin", new GameObjectTemplate(typeof(EnemyCamoGoblin), new object[] { 0 }));
            ObjectTemplates.Add("e_bonePutter", new GameObjectTemplate(typeof(EnemyBonePutter), new object[] { true }));
            ObjectTemplates.Add("e_karakoro", new GameObjectTemplate(typeof(EnemyKarakoro), new object[] { 0, null, null }));
            ObjectTemplates.Add("e_gibdo", new GameObjectTemplate(typeof(EnemyGibdo), new object[] { }));
            ObjectTemplates.Add("e_antiKirby", new GameObjectTemplate(typeof(EnemyAntiKirby), new object[] { }));
            ObjectTemplates.Add("e_vire", new GameObjectTemplate(typeof(EnemyVire), new object[] { }));
            ObjectTemplates.Add("e_rope", new GameObjectTemplate(typeof(EnemyRope), new object[] { }));
            ObjectTemplates.Add("e_flameFountain", new GameObjectTemplate(typeof(EnemyFlameFountain), new object[] { }));
            ObjectTemplates.Add("e_floorLayer", new GameObjectTemplate(typeof(EnemyFloorLayer), new object[] { 0, null }));
            ObjectTemplates.Add("e_rockSpawner", new GameObjectTemplate(typeof(EnemyRockSpawner), new object[] { 160, 128 }));
            ObjectTemplates.Add("e_anglerFry", new GameObjectTemplate(typeof(EnemyAnglerFry), new object[] { 0 }));


            ObjectTemplates.Add("break_enemies_end", null);
            ObjectTemplates.Add("break_2d_start", null);

            // 2d enemies
            ObjectTemplates.Add("e14", new GameObjectTemplate(typeof(EnemyGoomba), new object[] { }));
            ObjectTemplates.Add("e_CheepCheep", new GameObjectTemplate(typeof(EnemyCheepCheep), new object[] { 0, false }));
            ObjectTemplates.Add("e_Bloober", new GameObjectTemplate(typeof(EnemyBloober), new object[] { }));
            ObjectTemplates.Add("e_giantBubble", new GameObjectTemplate(typeof(EnemyGiantBubble), new object[] { }));
            ObjectTemplates.Add("e_thwimp", new GameObjectTemplate(typeof(EnemyThwimp), new object[] { }));

            ObjectTemplates.Add("e_PiranhaPlant", new GameObjectTemplate(typeof(EnemyPiranhaPlant), new object[] { }));
            ObjectTemplates.Add("e_MegaThwomp", new GameObjectTemplate(typeof(EnemyMegaThwomp), new object[] { }));
            ObjectTemplates.Add("e_SpikedThwomp", new GameObjectTemplate(typeof(EnemySpikedThwomp), new object[] { }));
            ObjectTemplates.Add("e_Podoboo", new GameObjectTemplate(typeof(EnemyPodoboo), new object[] { 0 }));

            ObjectTemplates.Add("break_2d_end", null);
            ObjectTemplates.Add("break_mbosses_start", null);

            // mini-bosses
            ObjectTemplates.Add("mb1", new GameObjectTemplate(typeof(MBossRollingBones), new object[] { null, null }));
            ObjectTemplates.Add("mb_king_moblin", new GameObjectTemplate(typeof(MKingMoblin), new object[] { null, null }));
            ObjectTemplates.Add("mb_hinox", new GameObjectTemplate(typeof(MBossHinox), new object[] { null, 0 }));
            ObjectTemplates.Add("mb_BallAndChainSoldier", new GameObjectTemplate(typeof(MBossBallAndChainSoldier), new object[] { null }));
            ObjectTemplates.Add("mb_dodongo_snake", new GameObjectTemplate(typeof(MDodongoSnake), new object[] { null, 0, false }));
            ObjectTemplates.Add("mb_desert_lanmola", new GameObjectTemplate(typeof(MBossDesertLanmola), new object[] { null, null }));
            ObjectTemplates.Add("mb_cue_ball", new GameObjectTemplate(typeof(MBossCueBall), new object[] { null, null }));
            ObjectTemplates.Add("mb_MasterStalfos", new GameObjectTemplate(typeof(MBossMasterStalfos), new object[] { null, 0 }));
            ObjectTemplates.Add("mb_Gohma", new GameObjectTemplate(typeof(MBossGohma), new object[] { null, false }));
            ObjectTemplates.Add("mb_ArmosKnight", new GameObjectTemplate(typeof(MBossArmosKnight), new object[] { null }));
            ObjectTemplates.Add("mb_Smasher", new GameObjectTemplate(typeof(MBossSmasher), new object[] { null }));
            ObjectTemplates.Add("mb_StoneHinox", new GameObjectTemplate(typeof(MBossStoneHinox), new object[] { null }));
            ObjectTemplates.Add("mb_GiantBuzzBlob", new GameObjectTemplate(typeof(MBossGiantBuzzBlob), new object[] { null }));
            ObjectTemplates.Add("mb_GrimCreeper", new GameObjectTemplate(typeof(MBossGrimCreeper), new object[] { null }));
            ObjectTemplates.Add("mb_TurtleRock", new GameObjectTemplate(typeof(MBossTurtleRock), new object[] { null }));
            ObjectTemplates.Add("mb_Blaino", new GameObjectTemplate(typeof(MBossBlaino), new object[] { null, null }));

            ObjectTemplates.Add("break_mbosses_end", null);
            ObjectTemplates.Add("break_nightmare_start", null);

            // nightmares
            ObjectTemplates.Add("nightmare-moldorm", new GameObjectTemplate(typeof(BossMoldorm), new object[] { null, null }));
            ObjectTemplates.Add("nightmare_genie", new GameObjectTemplate(typeof(BossGenieBottle), new object[] { null }));
            ObjectTemplates.Add("nightmare_slime_eye", new GameObjectTemplate(typeof(BossSlimeEye), new object[] { null, null, null }));
            ObjectTemplates.Add("nightmare_angler_fish", new GameObjectTemplate(typeof(BossAnglerFish), new object[] { null }));
            ObjectTemplates.Add("nightmare_slime_eel", new GameObjectTemplate(typeof(BossSlimeEelSpawn), new object[] { null }));
            ObjectTemplates.Add("facade", new GameObjectTemplate(typeof(BossFacade), new object[] { null, null }));
            ObjectTemplates.Add("nightmare_HardhitBeetle", new GameObjectTemplate(typeof(BossHardhitBeetle), new object[] { null }));
            ObjectTemplates.Add("nightmare_EvilEagle", new GameObjectTemplate(typeof(BossEvilEagle), new object[] { null }));
            ObjectTemplates.Add("nightmare_HotHead", new GameObjectTemplate(typeof(BossHotHead), new object[] { null }));
            ObjectTemplates.Add("nightmare", new GameObjectTemplate(typeof(BossFinalBoss), new object[] { null }));


            foreach (var objectTemplate in ObjectTemplates)
            {
                var name = objectTemplate.Key;
                var gameObjectTemplate = objectTemplate.Value;

                // editor break?
                if (gameObjectTemplate == null)
                    continue;

                foreach (var constructor in gameObjectTemplate.ObjectType.GetConstructors())
                {
                    var parameters = constructor.GetParameters();
                    // the system currently only supports constructors with 3 additional parameters (map, posX, posY)
                    if (gameObjectTemplate.Parameter.Length + 3 != parameters.Length)
                        continue;

                    var correctParameter = true;
                    for (var i = 3; i < parameters.Length; i++)
                    {
                        if (gameObjectTemplate.Parameter[i - 3] != null &&
                            parameters[i].ParameterType != gameObjectTemplate.Parameter[i - 3].GetType())
                        {
                            correctParameter = false;
                            break;
                        }
                    }

                    if (!correctParameter)
                        continue;

                    ObjectSpawner.Add(name, ObjActivator.GetActivator<GameObject>(constructor));

                    // parameter
                    GameObjectParameter.Add(name, constructor.GetParameters());
                }
            }

            if (Game1.EditorMode)
            {
                // create the gameObjects used in the editor
                var editorMap = Map.Map.CreateEmptyMap();
                foreach (var template in GameObjectTemplates.ObjectTemplates)
                {
                    if (template.Value == null)
                        continue;

                    // check if a base constructor exists and use this instead
                    if (template.Value.ObjectType.GetConstructor(Type.EmptyTypes) != null)
                        ObjectEditorScreen.EditorObjectTemplates.Add(template.Key, (GameObject)Activator.CreateInstance(template.Value.ObjectType));
                    else
                        ObjectEditorScreen.EditorObjectTemplates.Add(template.Key, ObjectManager.GetGameObject(editorMap,
                            template.Key, AddPositionToParameterArray(template.Value.Parameter, editorMap, 0, 0)));
                }
            }
        }

        private static object[] AddPositionToParameterArray(object[] objParameter, Map.Map map, int posX, int posY)
        {
            // object has only posX and posY as parameter
            if (objParameter == null)
                return new object[] { map, posX, posY };

            var outParameter = new object[objParameter.Length + 3];
            Array.Copy(objParameter, 0, outParameter, 3, objParameter.Length);
            outParameter[0] = map;
            outParameter[1] = posX;
            outParameter[2] = posY;

            return outParameter;
        }
    }
}
