﻿using RogueSharp;
using RogueSharpRLNetSamples.Actors;
using RogueSharpRLNetSamples.Interfaces;

namespace RogueSharpRLNetSamples.Abilities
{
   public class MagicMissile : Ability, ITargetable
   {
      private readonly int _attack;
      private readonly int _attackChance;

      public MagicMissile( int attack, int attackChance)
      {
         Name = "Magic Missile";
         TurnsToRefresh = 10;
         TurnsUntilRefreshed = 0;
         _attack = attack;
         _attackChance = attackChance;
      }

      protected override bool PerformAbility()
      {
         return Game.TargetingService.SelectMonster( this );
      }

      public void SelectTarget( Point target )
      {
         DungeonMap map = Game.CommandService.DungeonMap;
         Player player = map.GetPlayer();
         Monster monster = map.GetMonsterAt( target.X, target.Y );
         if ( monster != null )
         {
            Game.Messages.Add( $"{player.Name} casts a {Name} at {monster.Name}" );
            Actor magicMissleActor = new Actor
            {
               Attack = _attack, AttackChance = _attackChance, Name = Name
            };
            Game.CommandService.Attack( magicMissleActor, monster );
         }
      }
   }
}
