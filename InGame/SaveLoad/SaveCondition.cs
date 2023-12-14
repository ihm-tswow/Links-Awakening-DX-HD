using System.Collections.Generic;

namespace ProjectZ.InGame.SaveLoad
{
    // the parser does support and, or, negate and simple brackets
    // this is probably not how it should be done
    // I never wrote something like this before and did not look into how it is normally done...
    class SaveCondition
    {
        private static readonly Dictionary<int, string> BracketDictionary = new Dictionary<int, string>();

        public static void TestCondition()
        {
            Game1.GameManager.SaveManager.SetString("enter1", "1");
            Game1.GameManager.SaveManager.SetString("et1", "1");
            Game1.GameManager.SaveManager.SetString("enter2", "1");
            Game1.GameManager.SaveManager.SetString("et2", "0");

            var condition = GetConditionNode("(!enter1|et1)");
            var cCheck = condition.Check();
        }

        public static bool CheckCondition(string strCondition)
        {
            var condition = GetConditionNode(strCondition);
            return condition.Check();
        }

        public static ConditionNode GetConditionNode(string strCondition)
        {
            BracketDictionary.Clear();

            // prepass replaces elements in brackets with dummy elements
            strCondition = PreParse(strCondition);

            // build the normal left/right tree
            var node = ParseCondition(strCondition);

            // readd the bracket elements
            return PostParse(node);
        }

        private static string PreParse(string strCondition)
        {
            // does not support brackets inside brackets
            while (strCondition.Contains("("))
            {
                var indexOpen = strCondition.IndexOf('(');
                var indexClose = strCondition.IndexOf(')');

                var subString0 = strCondition.Substring(indexOpen + 1, indexClose - indexOpen - 1);

                var index = BracketDictionary.Count;
                BracketDictionary.Add(index, subString0);

                strCondition = strCondition.Remove(indexOpen, indexClose - indexOpen + 1);
                strCondition = strCondition.Insert(indexOpen, "#" + index);
            }

            return strCondition;
        }

        private static ConditionNode ParseCondition(string strCondition)
        {
            if (string.IsNullOrEmpty(strCondition))
                return new ConditionNode();

            if (strCondition.Contains("|"))
            {
                var subString = strCondition.Split(new[] { '|' }, 2);

                var condition = new CNodeOr(
                    ParseCondition(subString[0]),
                    ParseCondition(subString[1]));

                return condition;
            }

            if (strCondition.Contains("&"))
            {
                var subString = strCondition.Split(new[] { '&' }, 2);

                var condition = new CNodeAnd(
                    ParseCondition(subString[0]),
                    ParseCondition(subString[1]));

                return condition;
            }

            return new CNode(strCondition.Replace("!", ""), strCondition.Contains('!'));
        }

        private static ConditionNode PostParse(ConditionNode conditionNode)
        {
            if (conditionNode is CNode cNode)
                if (cNode.SaveKey.Contains('#'))
                {
                    var index = int.Parse(cNode.SaveKey.Replace("#", ""));
                    conditionNode = ParseCondition(BracketDictionary[index]);
                }
            if (conditionNode.Left is CNode nodeLeft)
                if (nodeLeft.SaveKey.Contains('#'))
                {
                    var index = int.Parse(nodeLeft.SaveKey.Replace("#", ""));
                    conditionNode.Left = ParseCondition(BracketDictionary[index]);
                }
            if (conditionNode.Right is CNode nodeRight)
                if (nodeRight.SaveKey.Contains('#'))
                {
                    var index = int.Parse(nodeRight.SaveKey.Replace("#", ""));
                    conditionNode.Right = ParseCondition(BracketDictionary[index]);
                }

            if (conditionNode.Left != null)
                conditionNode.Left = PostParse(conditionNode.Left);
            if (conditionNode.Right != null)
                conditionNode.Right = PostParse(conditionNode.Right);

            return conditionNode;
        }
    }

    class ConditionNode
    {
        public ConditionNode Left;
        public ConditionNode Right;

        public virtual bool Check() => false;
    }

    class CNode : ConditionNode
    {
        public string SaveKey;
        public string Condition;
        public bool Negate;

        public CNode(string saveKey, bool negate)
        {
            if (saveKey.Contains("="))
            {
                var split = saveKey.Split("=");
                SaveKey = split[0];
                Condition = split[1];
            }
            else
            {
                SaveKey = saveKey;
                Condition = "1";
            }
            Negate = negate;
        }

        public override bool Check()
        {
            return Negate ^ (Game1.GameManager.SaveManager.GetString(SaveKey, "0") == Condition);
        }
    }

    class CNodeAnd : ConditionNode
    {
        public CNodeAnd(ConditionNode left, ConditionNode right)
        {
            Left = left;
            Right = right;
        }

        public override bool Check()
        {
            return Left.Check() && Right.Check();
        }
    }

    class CNodeOr : ConditionNode
    {
        public CNodeOr(ConditionNode left, ConditionNode right)
        {
            Left = left;
            Right = right;
        }

        public override bool Check()
        {
            return Left.Check() || Right.Check();
        }
    }
}
