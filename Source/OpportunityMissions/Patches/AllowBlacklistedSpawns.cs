using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech.Data;
using Harmony;
using HBS.Collections;

namespace OpportunityMissions.Patches
{
    [HarmonyPatch(typeof(Tags_MDDExtenstions), "GetTagSetWithRequiredTags")]
    public static class Tags_MDDExtenstions_GetTagSetWithRequiredTags_Patch
    {
        public static void Postfix(ref List<TagSet_MDD> __result, TagSetType tagSetType, TagSet requiredTags, TagSet excludedTags)
        {
            try
            {
                //MAD:
                string overrideBlacklistedTag = ("ignoreTag-BLACKLISTED").ToLower();
                if (!requiredTags.Contains(overrideBlacklistedTag))
                {
                    return;
                }
                //:DAM

                MetadataDatabase mdd = MetadataDatabase.Instance;

                TagSet tags = new TagSet(requiredTags);
                TagSet tagSet = new TagSet(excludedTags);
                if (!tagSet.Contains(Tags_MDDExtenstions.BLACKLISTED_TAG))
                {
                    tagSet.Add(Tags_MDDExtenstions.BLACKLISTED_TAG);
                }

                //MAD:
                if (tags.Contains(overrideBlacklistedTag))
                {
                    Logger.Debug($"[Tags_MDDExtenstions_GetTagSetWithRequiredTags_PREFIX] Request has ignoreTag-BLACKLISTED tag set. Removing BLACKLISTED tag");
                    tagSet.Remove(Tags_MDDExtenstions.BLACKLISTED_TAG);
                    // Need to remove the custom tag too, otherwise the query below will include it as required -> no result -> fallback unit (cicada)
                    tags.Remove(overrideBlacklistedTag);

                    Logger.Info($"[SimGameState_ResolveCompleteContract_PREFIX] requiredTags: {String.Join(", ", tags.ToArray())}");
                    Logger.Info($"[SimGameState_ResolveCompleteContract_PREFIX] excludedTags: {String.Join(", ", tagSet.ToArray())}");
                }
                //:DAM

                List<Tag_MDD> orCreateTagsInTagSet = mdd.GetOrCreateTagsInTagSet(tags);
                List<Tag_MDD> orCreateTagsInTagSet2 = mdd.GetOrCreateTagsInTagSet(tagSet);
                string text = "SELECT ts.* FROM TagSet ts ";
                string text2 = string.Empty;
                string text3 = string.Format("WHERE ts.TagSetTypeId = {0} ", (int)tagSetType);
                for (int i = 0; i < orCreateTagsInTagSet.Count; i++)
                {
                    text2 += string.Format(Tags_MDDExtenstions.GTSWRT_RequiredInnerJoinFormat, i);
                    text3 += string.Format(Tags_MDDExtenstions.GTSWRT_RequiredWhereClauseFormat, i, orCreateTagsInTagSet[i].Name);
                }
                for (int j = 0; j < orCreateTagsInTagSet2.Count; j++)
                {
                    text2 += string.Format(Tags_MDDExtenstions.GTSWRT_ExcludedLeftJoinFormat, j, orCreateTagsInTagSet2[j].Name);
                    text3 += string.Format(Tags_MDDExtenstions.GTSWRT_ExcludedWhereClauseFormat, j);
                }
                text = text + text2 + text3 + " COLLATE NOCASE ";

                __result =  mdd.Query<TagSet_MDD>(text, null, null, true, null, null).ToList<TagSet_MDD>();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
