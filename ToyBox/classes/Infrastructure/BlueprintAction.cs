﻿using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Armies;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.Armies.TacticalCombat.Blueprints;
using Kingmaker.Armies.TacticalCombat.Brain;
using Kingmaker.Armies.TacticalCombat.Brain.Considerations;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Credits;
using Kingmaker.Blueprints.Encyclopedia;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Blueprints.Console;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Interaction;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Tutorial;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Customization;
using Kingmaker.Utility;
using Kingmaker.Visual.Sound;

namespace ToyBox {

    public static class BlueprintExensions {
        public static List<BlueprintAction> ActionsForUnit(this BlueprintScriptableObject bp, UnitEntityData ch) {
            var results = new List<BlueprintAction>();
            Type type = bp.GetType();
            if (ch.IsMainCharacter) {
                foreach (var action in BlueprintAction.globalActions) {
                    if (type.IsKindOf(action.type) && action.canPerform(ch, bp)) { results.Add(action); }
                }
            }
            foreach (var action in BlueprintAction.characterActions) {
                if (type.IsKindOf(action.type) && action.canPerform(ch, bp)) { results.Add(action); }
            }
            return results;
        }
    }
    public class BlueprintAction : NamedMutator<UnitEntityData, BlueprintScriptableObject> {
        public BlueprintAction(
            String name,
            Type type,
            Action<UnitEntityData, BlueprintScriptableObject> action,
            Func<UnitEntityData, BlueprintScriptableObject, bool> canPerform = null
            ) : base(name, type, action, canPerform) { }


        public static BlueprintAction[] globalActions = new BlueprintAction[] {
            new BlueprintAction("Add", typeof(BlueprintItem),
                (ch, bp) => { ch.Inventory.Add((BlueprintItem)bp, 1, null); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintItem),
                (ch, bp) => { ch.Inventory.Remove((BlueprintItem)bp, 1); },
                (ch, bp) => { return ch.Inventory.Contains((BlueprintItem)bp);  }
                ),
            new BlueprintAction("Spawn", typeof(BlueprintUnit),
                (ch, bp) => { Actions.SpawnUnit((BlueprintUnit)bp); }
                ),
#if false
            new BlueprintAction("Remove", typeof(BlueprintUnit),
                (ch, bp) => {CheatsCombat.Kill((BlueprintUnit)bp); },
                (ch, bp) => { return ch.Inventory.Contains((BlueprintUnit)bp);  }
                ),
#endif
        };

        public static BlueprintAction[] characterActions = new BlueprintAction[] {
            // Features
            new BlueprintAction("Add", typeof(BlueprintFeature),
                (ch, bp) => { ch.Descriptor.AddFact((BlueprintUnitFact)bp); },
                (ch, bp) => { return !ch.Progression.Features.HasFact((BlueprintUnitFact)bp); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintFeature),
                (ch, bp) => { ch.Progression.Features.RemoveFact((BlueprintUnitFact)bp); },
                (ch, bp) => { return ch.Progression.Features.HasFact((BlueprintUnitFact)bp);  }
                ),
            // Buffs
            new BlueprintAction("Add", typeof(BlueprintBuff),
                (ch, bp) => { GameHelper.ApplyBuff(ch,(BlueprintBuff)bp); },
                (ch, bp) => { return !ch.Descriptor.Buffs.HasFact((BlueprintUnitFact)bp); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintBuff),
                (ch, bp) => { ch.Descriptor.RemoveFact((BlueprintUnitFact)bp); },
                (ch, bp) => { return ch.Descriptor.Buffs.HasFact((BlueprintBuff)bp);  }
                ),
            // Races

            // Abilities
#if false
            new BlueprintAction("Add", typeof(BlueprintAbility),
                (ch, bp) => { ch.Progression   GameHelper.ApplyBuff(ch,(BlueprintBuff)bp); },
                (ch, bp) => { return !ch.Descriptor.Abilities.HasFact((BlueprintUnitFact)bp); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintAbility),
                (ch, bp) => { ch.Descriptor.RemoveFact((BlueprintUnitFact)bp); },
                (ch, bp) => { return ch.Descriptor.Buffs.HasFact((BlueprintBuff)bp);  }
                ),
#endif
        };

        public static int maxActions() { return globalActions.Count() + characterActions.Count(); }
        public static int maxCharacterActions() { return characterActions.Count(); }
#if false
        public static HashSet<Type> ignoredBluePrintTypes = new HashSet<Type> {
                typeof(BlueprintAiCastSpell),
                typeof(BlueprintAnswer),
                typeof(BlueprintAnswersList),
                typeof(BlueprintAreaEnterPoint),
                typeof(BlueprintBarkBanter),
                typeof(BlueprintCue),
                typeof(BlueprintCueSequence),
                typeof(BlueprintDialog),
                typeof(BlueprintGlobalMapEdge),
                typeof(BlueprintGlobalMapPoint),
                typeof(BlueprintQuest),
                typeof(BlueprintSequenceExit),
                typeof(BlueprintTutorial),
                typeof(BlueprintTacticalCombatObstaclesMap),
                typeof(BlueprintCheck),
                typeof(BlueprintScriptZone),
                typeof(BlueprintAreaMechanics),
                typeof(BlueprintTacticalCombatAiCastSpell),
                typeof(TacticalCombatTagConsideration),
                typeof(BlueprintUnitAsksList),
                typeof(BlueprintCreditsRoles),
                typeof(CommandCooldownConsideration),
                typeof(LifeStateConsideration),
                typeof(BlueprintAiAttack),
                typeof(BlueprintAiSwitchWeapon),
                typeof(BlueprintCompanionStory),
                typeof(CanMakeFullAttackConsideration),
                typeof(DistanceConsideration),
                typeof(FactConsideration),
                typeof(NotImpatientConsideration),
                typeof(BlueprintGlobalMapPointVariation),
                typeof(BlueprintControllableProjectile),
                typeof(BlueprintInteractionRoot),
                typeof(UnitsThreateningConsideration),
                typeof(BlueprintTacticalCombatAiAttack),
                typeof(UnitsAroundConsideration),
                typeof(LastTargetConsideration),
                typeof(TargetSelfConsideration),
                typeof(BlueprintAiFollow),
                typeof(TargetClassConsideration),
                typeof(HealthConsideration),
                typeof(LineOfSightConsideration),
                typeof(RaceGenderDistribution),
                typeof(ArmorTypeConsideration),
                typeof(ComplexConsideration),
                typeof(ManualTargetConsideration),
                typeof(BlueprintDungeonLocalizedStrings),
                typeof(DistanceRangeConsideration),
                typeof(AlignmentConsideration),
                typeof(GamePadIcons),
                typeof(GamePadTexts),
                typeof(CasterClassConsideration),
                typeof(StatConsideration),
                typeof(BlueprintAiTouch),
                typeof(ActiveCommandConsideration),
                typeof(ArmyHealthConsideration),
                typeof(BuffsAroundConsideration),
                typeof(BuffConsideration),
                typeof(HealthAroundConsideration),
                typeof(HitThisRoundConsideration),
                typeof(ThreatedByConsideration),
                typeof(CanUseSpellCombatConsideration),
                typeof(CustomAiConsiderationsRoot),
                typeof(BlueprintQuestObjective),
                typeof(BlueprintScriptZone),
                typeof(BlueprintQuestGroups),
                typeof(Cutscene),
                typeof(Consideration),
                typeof(BlueprintEtude),
                typeof(BlueprintSummonPool),
                typeof(BlueprintUnit),
                typeof(BlueprintArea),
                typeof(BlueprintArmorEnchantment),
                typeof(BlueprintWeaponEnchantment),
                typeof(BlueprintEquipmentEnchantment),
                typeof(BlueprintEncyclopediaPage),
                typeof(BlueprintAreaPart),
                typeof(BlueprintLogicConnector),
                typeof(BlueprintKingdomBuff),
                typeof(BlueprintSettlementBuilding),
            };
#endif
    }
}