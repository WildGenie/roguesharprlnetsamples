﻿using System.Collections.Generic;
using RogueSharp;
using RogueSharpRLNetSamples.Actors;

namespace RogueSharpRLNetSamples.Abilities
{
   public class Whirlwind : Ability
   {
      public Whirlwind()
      {
         Name = "Whirlwind";
         TurnsToRefresh = 20;
         TurnsUntilRefreshed = 0;
      }

      protected override bool PerformAbility()
      {
         DungeonMap map = Game.CommandService.DungeonMap;

         Player player = map.GetPlayer();
         Game.Messages.Add( $"{player.Name} performs a whirlwind attack against all adjacent enemies" );

         List<Point> monsterLocations = new List<Point>();

         foreach ( Cell cell in map.GetCellsInArea( player.X, player.Y, 1 ) )
         {
            foreach ( Point monsterLocation in map.GetMonsterLocations() )
            {
               if ( cell.X == monsterLocation.X && cell.Y == monsterLocation.Y )
               {
                  monsterLocations.Add( monsterLocation );
               }
            }
         }

         foreach ( Point monsterLocation in monsterLocations )
         {
            Monster monster = map.MonsterAt( monsterLocation.X, monsterLocation.Y );
            if ( monster != null )
            {
               Game.CommandService.Attack( player, monster );
            }
         }

         return true;
      }
   }
}
