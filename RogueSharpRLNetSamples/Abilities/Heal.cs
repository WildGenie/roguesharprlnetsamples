﻿using System;
using RogueSharpRLNetSamples.Actors;

namespace RogueSharpRLNetSamples.Abilities
{
   public class Heal : Ability
   {
      private readonly int _amountToHeal;

      public Heal( int amountToHeal )
      {
         Name = "Heal Self";
         TurnsToRefresh = 60;
         TurnsUntilRefreshed = 0;
         _amountToHeal = amountToHeal;
      }

      protected override bool PerformAbility()
      {
         Player player = Game.CommandService.DungeonMap.GetPlayer();

         player.Health = Math.Min( player.MaxHealth, player.Health + _amountToHeal );

         return true;
      }
   }
}
