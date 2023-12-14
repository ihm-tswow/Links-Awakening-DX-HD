using System.Collections.Generic;

namespace ProjectZ.InGame.Things
{
    public class ItemManager
    {
        public Dictionary<string, GameItem> Items => _items;

        public GameItem this[string key] => key != null && _items.ContainsKey(key) ? _items[key] : null;

        private readonly Dictionary<string, GameItem> _items = new Dictionary<string, GameItem>();

        public void Load()
        {
            // TODO_Opt: load all the items from a file

            // dungeon
            // same keys but with different sounds and one does show the description
            _items.Add("smallkey", new GameItem(
                Resources.GetSprite("smallkey"),
                name: "smallkey",
                count: 1,
                maxCount: 9,
                drawLength: 1,
                soundEffectName: "D370-01-01"
            ));
            _items.Add("smallkeyChest", new GameItem(
                Resources.GetSprite("smallkey"),
                name: "smallkey",
                pickUpDialog: "smallkey",
                count: 1,
                drawLength: 1,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("nightmarekey", new GameItem(
                Resources.GetSprite("nightmarekey"),
                name: "nightmarekey",
                maxCount: 1,
                pickUpDialog: "nightmarekey",
                soundEffectName: "D360-01-01",
                turnDownMusic: true,
                level: -1
            ));
            _items.Add("compass", new GameItem(
                Resources.GetSprite("compass"),
                name: "compass",
                count: 1,
                maxCount: 1,
                pickUpDialog: "compass",
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("dmap", new GameItem(
                Resources.GetSprite("dmap"),
                name: "dmap",
                count: 1,
                maxCount: 1,
                pickUpDialog: "dmap",
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("stonebeak", new GameItem(
                Resources.GetSprite("stonebeak"),
                name: "stonebeak",
                count: 1,
                maxCount: 1,
                pickUpDialog: "stonebeak",
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));

            _items.Add("potion", new GameItem(
                Resources.GetSprite("potion"),
                name: "potion",
                count: 1,
                maxCount: 1,
                pickUpDialog: "potion",
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("potion_show", new GameItem(
                name: "potion",
                count: 1,
                showAnimation: 1,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("shell", new GameItem(
                Resources.GetSprite("shell"),
                Resources.GetSprite("shellMap"),
                name: "shell",
                pickUpDialog: "seashell",
                count: 1,
                soundEffectName: "D370-01-01"
            ));
            _items.Add("shellChest", new GameItem(
                Resources.GetSprite("shell"),
                Resources.GetSprite("shellMap"),
                name: "shell",
                pickUpDialog: "seashell",
                count: 1,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("shellPresent", new GameItem(
                Resources.GetSprite("shell_present"),
                name: "shell",
                pickUpDialog: "seashell",
                count: 1,
                showAnimation: 3,
                soundEffectName: "D370-01-01"
            ));

            // not sure why there are two differently colored versions
            // I am using the same version ingame and in the menu
            _items.Add("goldLeaf", new GameItem(
                Resources.GetSprite("goldLeaf"), // icon used ingame, in the menu the less colorfull version is used
                name: "goldLeaf",
                pickUpDialog: "goldLeaf",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                showAnimation: 1,
                showTime: 1500
            ));

            // instruments
            _items.Add("instrument0", new GameItem(
                Resources.GetSprite("instrument0"),
                name: "instrument0",
                pickUpDialog: "instrument0",
                count: 1,
                maxCount: 1,
                isRelict: true,
                showAnimation: 1,
                showEffect: true,
                showTime: 1500
            ));
            _items.Add("instrument1", new GameItem(
                Resources.GetSprite("instrument1"),
                name: "instrument1",
                pickUpDialog: "instrument1",
                count: 1,
                maxCount: 1,
                isRelict: true,
                showAnimation: 1,
                showEffect: true,
                showTime: 1500
            ));
            _items.Add("instrument2", new GameItem(
                Resources.GetSprite("instrument2"),
                name: "instrument2",
                pickUpDialog: "instrument2",
                count: 1,
                maxCount: 1,
                isRelict: true,
                showAnimation: 1,
                showEffect: true,
                showTime: 1500
            ));
            _items.Add("instrument3", new GameItem(
                Resources.GetSprite("instrument3"),
                name: "instrument3",
                pickUpDialog: "instrument3",
                count: 1,
                maxCount: 1,
                isRelict: true,
                showAnimation: 1,
                showEffect: true,
                showTime: 1500
            ));
            _items.Add("instrument4", new GameItem(
                Resources.GetSprite("instrument4"),
                name: "instrument4",
                pickUpDialog: "instrument4",
                count: 1,
                maxCount: 1,
                isRelict: true,
                showAnimation: 1,
                showEffect: true,
                showTime: 1500
            ));
            _items.Add("instrument5", new GameItem(
                Resources.GetSprite("instrument5"),
                name: "instrument5",
                pickUpDialog: "instrument5",
                count: 1,
                maxCount: 1,
                isRelict: true,
                showAnimation: 1,
                showEffect: true,
                showTime: 1500
            ));
            _items.Add("instrument6", new GameItem(
                Resources.GetSprite("instrument6"),
                name: "instrument6",
                pickUpDialog: "instrument6",
                count: 1,
                maxCount: 1,
                isRelict: true,
                showAnimation: 1,
                showEffect: true,
                showTime: 1500
            ));
            _items.Add("instrument7", new GameItem(
                Resources.GetSprite("instrument7"),
                name: "instrument7",
                pickUpDialog: "instrument7",
                count: 1,
                maxCount: 1,
                isRelict: true,
                showAnimation: 1,
                showEffect: true,
                showTime: 1500
            ));

            // trade items
            _items.Add("trade0", new GameItem(
                Resources.GetSprite("trade0"),
                name: "trade0",
                pickUpDialog: "yoshiPickup",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade1", new GameItem(
                Resources.GetSprite("trade1"),
                name: "trade1",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade2", new GameItem(
                Resources.GetSprite("trade2"),
                name: "trade2",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade3", new GameItem(
                Resources.GetSprite("trade3"),
                name: "trade3",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade4", new GameItem(
                Resources.GetSprite("trade4"),
                name: "trade4",
                pickUpDialog: "trade4",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade5", new GameItem(
                Resources.GetSprite("trade5"),
                mapSprite: Resources.GetSprite("trade5Map"),
                name: "trade5",
                pickUpDialog: "trade5",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade6", new GameItem(
                Resources.GetSprite("trade6"),
                name: "trade6",
                pickUpDialog: "trade6Collected",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade7", new GameItem(
                Resources.GetSprite("trade7"),
                name: "trade7",
                pickUpDialog: "trade7",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade8", new GameItem(
                Resources.GetSprite("trade8"),
                name: "trade8",
                pickUpDialog: "trade8",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade9", new GameItem(
                // shown icon is browner
                Resources.GetSprite("trade9"),
                name: "trade9",
                pickUpDialog: "trade9",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade10", new GameItem(
                Resources.GetSprite("trade10"),
                name: "trade10",
                pickUpDialog: "trade10",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade11", new GameItem(
                Resources.GetSprite("trade11"),
                name: "trade11",
                pickUpDialog: "trade11",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade12", new GameItem(
                Resources.GetSprite("trade12"),
                name: "trade12",
                pickUpDialog: "trade12",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("trade13", new GameItem(
                Resources.GetSprite("trade13"),
                name: "trade13",
                pickUpDialog: "trade13",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));

            _items.Add("marin", new GameItem(
                Resources.GetSprite("marin_item"),
                name: "marin",
                pickUpDialog: "maria_collected",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                showAnimation: 1
            ));
            _items.Add("rooster", new GameItem(
                Resources.GetSprite("marin_item"),
                name: "rooster",
                pickUpDialog: "rooster",
                count: 1,
                maxCount: 1
            ));
            _items.Add("ghost", new GameItem(
                Resources.GetSprite("marin_item"),
                name: "ghost",
                count: 1,
                maxCount: 1
            ));

            // overworld
            // TODO: look into the colors
            _items.Add("ruby", new GameItem(
                Resources.GetSprite("rubyBlue"),
                name: "ruby",
                count: 1,
                maxCount: 999,
                soundEffectName: "D370-05-05"
            ));
            _items.Add("rubyGreen", new GameItem(
                Resources.GetSprite("rubyGreen"),
                animateSprite: true,
                name: "ruby",
                count: 5,
                soundEffectName: "D370-05-05"
            ));
            // TODO: shouldnt red be 30?
            _items.Add("ruby5", new GameItem(
                Resources.GetSprite("rubyRed"),
                name: "ruby",
                count: 5,
                soundEffectName: "D370-05-05"
            ));
            _items.Add("ruby10", new GameItem(
                Resources.GetSprite("rubyBlue"),
                name: "ruby",
                count: 10,
                soundEffectName: "D370-05-05"
            ));
            _items.Add("ruby20", new GameItem(
                name: "ruby",
                pickUpDialog: "ruby20",
                count: 20,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            // trendy game ruby
            _items.Add("ruby30", new GameItem(
                Resources.GetSprite("rubyRed"),
                name: "ruby",
                pickUpDialog: "ruby30",
                count: 30,
                soundEffectName: "D370-05-05"
            ));
            _items.Add("ruby50", new GameItem(
                name: "ruby",
                pickUpDialog: "ruby50",
                count: 50,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("ruby100", new GameItem(
                name: "ruby",
                pickUpDialog: "ruby100",
                count: 100,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("ruby200", new GameItem(
                Resources.GetSprite("rubyBlue"),
                name: "ruby",
                pickUpDialog: "ruby200",
                count: 200,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));

            _items.Add("heart", new GameItem(
                Resources.GetSprite("heart"),
                name: "heart",
                count: 1,
                maxCount: 999,
                soundEffectName: "D370-06-06"
            ));
            _items.Add("heart_1", new GameItem(
                Resources.GetSprite("heart"),
                name: "heart",
                pickUpDialog: "heart",
                count: 1,
                maxCount: 999,
                soundEffectName: "D370-06-06"
            ));
            _items.Add("heart_3", new GameItem(
                Resources.GetSprite("heart"),
                name: "heart",
                count: 3,
                maxCount: 999,
                soundEffectName: "D370-01-01"
            ));

            _items.Add("heartMeter", new GameItem(
                Resources.GetSprite("heartMeter"),
                name: "heartMeter",
                pickUpDialog: "heartMeter",
                count: 1,
                maxCount: 99,
                showAnimation: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("heartMeterSilent", new GameItem(
                name: "heartMeter",
                count: 1
            ));
            _items.Add("heartMeterFull", new GameItem(
                Resources.GetSprite("heartMeterFull"),
                name: "heartMeter",
                count: 4,
                showAnimation: 1,
                showTime: 1750,
                pickUpDialog: "heartMeterFull"
            ));

            // dungeon keys
            _items.Add("dkey1", new GameItem(
                Resources.GetSprite("dkey1"),
                name: "dkey1",
                pickUpDialog: "dkey1",
                count: 1,
                maxCount: 1,
                showAnimation: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("dkey2", new GameItem(
                Resources.GetSprite("dkey2"),
                name: "dkey2",
                pickUpDialog: "dkey2",
                count: 1,
                maxCount: 1,
                showAnimation: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("dkey3", new GameItem(
                Resources.GetSprite("dkey3"),
                name: "dkey3",
                pickUpDialog: "dkey3",
                count: 1,
                maxCount: 1,
                showAnimation: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("dkey4", new GameItem(
                Resources.GetSprite("dkey4"),
                name: "dkey4",
                pickUpDialog: "dkey4",
                count: 1,
                maxCount: 1,
                showAnimation: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("dkey5", new GameItem(
                Resources.GetSprite("dkey5"),
                name: "dkey5",
                pickUpDialog: "dkey5",
                count: 1,
                maxCount: 1,
                showAnimation: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));

            _items.Add("guardianAcorn", new GameItem(
                Resources.GetSprite("guardianAcorn"),
                name: "guardianAcorn",
                pickUpDialog: "guardianAcorn",
                showAnimation: 2,
                soundEffectName: "D360-23-17"
            ));
            _items.Add("pieceOfPower", new GameItem(
                Resources.GetSprite("pieceOfPower"),
                name: "pieceOfPower",
                pickUpDialog: "pieceOfPower",
                showAnimation: 2,
                soundEffectName: "D360-23-17"
            ));
            _items.Add("sword1PoP", new GameItem(
                Resources.GetSprite("sword1"),
                name: "sword1PoP",
                pickUpDialog: "pieceOfPower",
                showAnimation: 2,
                soundEffectName: "D360-23-17"
            ));
            _items.Add("sword2PoP", new GameItem(
                Resources.GetSprite("sword2"),
                name: "sword2PoP",
                pickUpDialog: "pieceOfPower",
                showAnimation: 2,
                soundEffectName: "D360-23-17"
            ));

            // level: = 0   => item as count
            // level > 0    => item has level
            // else         => item has nothing
            // accessories
            _items.Add("sword1", new GameItem(
                Resources.GetSprite("sword1"),
                name: "sword1",
                pickUpDialog: "sword1Collected",
                count: 1,
                maxCount: 1,
                level: 1,
                showAnimation: 1,
                equipable: true,
                showEffect: true,
                showTime: 4000
            ));
            _items.Add("sword2", new GameItem(
                Resources.GetSprite("sword2"),
                mapSprite: Resources.GetSprite("swordSpawn"),
                name: "sword2",
                count: 1,
                maxCount: 1,
                level: 2,
                showAnimation: 2,
                equipable: true
            ));
            _items.Add("shield", new GameItem(
                Resources.GetSprite("shield"),
                name: "shield",
                pickUpDialog: "shield_intro",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                level: 1,
                showAnimation: 1,
                equipable: true
            ));
            _items.Add("shield0", new GameItem(
                Resources.GetSprite("shield"),
                name: "shield",
                pickUpDialog: "shield",
                soundEffectName: "D370-01-01",
                count: 1,
                maxCount: 1,
                level: 1,
                equipable: true
            ));
            _items.Add("shieldBack", new GameItem(
                Resources.GetSprite("shield"),
                name: "shield",
                pickUpDialog: "shield_back",
                soundEffectName: "D370-01-01",
                count: 1,
                maxCount: 1,
                level: 1,
                equipable: true
            ));
            _items.Add("mirrorShield", new GameItem(
                Resources.GetSprite("mirror shield"),
                name: "mirrorShield",
                pickUpDialog: "mirrorShield",
                soundEffectName: "D368-16-10",
                turnDownMusic: true,
                count: 1,
                maxCount: 1,
                level: 2,
                showAnimation: 1,
                equipable: true
            ));
            _items.Add("toadstool", new GameItem(
                Resources.GetSprite("toadstool"),
                Resources.GetSprite("toadstoolMap"),
                name: "toadstool",
                pickUpDialog: "toadstool",
                count: 1,
                maxCount: 1,
                level: -1,
                showAnimation: 1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("feather", new GameItem(
                Resources.GetSprite("feather"),
                name: "feather",
                pickUpDialog: "feather",
                count: 1,
                maxCount: 1,
                level: -1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("stonelifter", new GameItem(
                Resources.GetSprite("stonelifter0"),
                name: "stonelifter",
                pickUpDialog: "bracelet0",
                count: 1,
                maxCount: 1,
                level: 1,
                showAnimation: 1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("stonelifter2", new GameItem(
                Resources.GetSprite("stonelifter1"),
                // base is not supported for different sprites
                name: "stonelifter2",
                pickUpDialog: "bracelet1",
                count: 1,
                maxCount: 1,
                level: 2,
                showAnimation: 1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("pegasusBoots", new GameItem(
                Resources.GetSprite("pegasusBoots"),
                name: "pegasusBoots",
                pickUpDialog: "pegasusBoots",
                count: 1,
                maxCount: 1,
                level: -1,
                //showAnimation: 1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("shovel", new GameItem(
                Resources.GetSprite("shovel"),
                name: "shovel",
                pickUpDialog: "shovel",
                count: 1,
                maxCount: 1,
                level: -1,
                showAnimation: 1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("flippers", new GameItem(
                Resources.GetSprite("flippers"),
                name: "flippers",
                pickUpDialog: "flippers",
                count: 1,
                maxCount: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("magicRod", new GameItem(
                Resources.GetSprite("magicRod"),
                name: "magicRod",
                pickUpDialog: "magicRod",
                count: 1,
                maxCount: 1,
                level: -1,
                showAnimation: 1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("ocarina", new GameItem(
                Resources.GetSprite("ocarina"),
                name: "ocarina",
                pickUpDialog: "ocarina",
                count: 1,
                maxCount: 1,
                level: -1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("ocarina_frog", new GameItem(
                Resources.GetSprite("ocarina"),
                name: "ocarina_frog",
                pickUpDialog: "ocarina_frog_collected",
                count: 1,
                maxCount: 1,
                showAnimation: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("ocarina_maria", new GameItem(
                Resources.GetSprite("ocarina"),
                name: "ocarina_maria",
                pickUpDialog: "ocarina_maria_collected",
                count: 1,
                maxCount: 1,
                showAnimation: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("ocarina_manbo", new GameItem(
                Resources.GetSprite("ocarina"),
                name: "ocarina_manbo",
                pickUpDialog: "ocarina_manbo_collected",
                count: 1,
                maxCount: 1,
                showAnimation: 1,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("hookshot", new GameItem(
                Resources.GetSprite("hookshot"),
                name: "hookshot",
                pickUpDialog: "hookshot",
                count: 1,
                maxCount: 1,
                level: -1,
                showAnimation: 1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));
            _items.Add("boomerang", new GameItem(
                Resources.GetSprite("boomerang"),
                name: "boomerang",
                pickUpDialog: "boomerang",
                count: 1,
                maxCount: 1,
                level: -1,
                showAnimation: 1,
                equipable: true,
                soundEffectName: "D368-16-10",
                turnDownMusic: true
            ));

            _items.Add("powder", new GameItem(
                Resources.GetSprite("powder"),
                name: "powder",
                count: 20,
                maxCount: 20,
                level: 0,
                equipable: true,
                soundEffectName: "D370-01-01",
                turnDownMusic: true
            ));
            _items.Add("powderTrendy", new GameItem(
                Resources.GetSprite("powder"),
                soundEffectName: "D370-01-01",
                name: "powder",
                pickUpDialog: "powder",
                count: 10
            ));
            _items.Add("powder_1", new GameItem(
                name: "powder",
                count: 1,
                soundEffectName: "D370-01-01"
            ));
            _items.Add("powder_10", new GameItem(
                name: "powder",
                count: 10,
                soundEffectName: "D370-01-01"
            ));
            _items.Add("powderPD", new GameItem(
                name: "powder",
                pickUpDialog: "powder",
                count: 20,
                showAnimation: 2,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("bomb", new GameItem(
                Resources.GetSprite("bomb"),
                name: "bomb",
                pickUpDialog: "bomb",
                count: 10,
                maxCount: 30,
                level: 0,
                soundEffectName: "D370-01-01",
                equipable: true
            ));
            _items.Add("bombChest", new GameItem(
                name: "bomb",
                pickUpDialog: "bomb",
                count: 1,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("bomb_1", new GameItem(
                name: "bomb",
                count: 1,
                soundEffectName: "D370-01-01"
            ));
            _items.Add("bomb_10", new GameItem(
                name: "bomb",
                count: 10,
                soundEffectName: "D370-01-01"
            ));
            _items.Add("bow", new GameItem(
                Resources.GetSprite("bow"),
                name: "bow",
                count: 10,
                maxCount: 30,
                level: 0,
                equipable: true
            ));
            _items.Add("arrow", new GameItem(
                Resources.GetSprite("arrow"),
                name: "arrow",
                count: 10,
                soundEffectName: "D370-01-01"
            ));
            _items.Add("arrow_1", new GameItem(
                Resources.GetSprite("arrow"),
                name: "arrow",
                count: 1,
                soundEffectName: "D370-01-01"
            ));

            _items.Add("cloakRed", new GameItem(
                Resources.GetSprite("cloak"),
                name: "cloakRed",
                pickUpDialog: "cloak_red",
                count: 1,
                maxCount: 1,
                level: -1,
                showAnimation: 1,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
            _items.Add("cloakBlue", new GameItem(
                Resources.GetSprite("cloak"),
                name: "cloakBlue",
                pickUpDialog: "cloak_blue",
                count: 1,
                maxCount: 1,
                level: -1,
                showAnimation: 1,
                soundEffectName: "D360-01-01",
                turnDownMusic: true
            ));
        }
    }
}
