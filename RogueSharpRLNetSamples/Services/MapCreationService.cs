using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp;
using RogueSharp.DiceNotation;
using RogueSharp.Random;
using RogueSharpRLNetSamples.Actors;
using RogueSharpRLNetSamples.Inventory;

namespace RogueSharpRLNetSamples.Services
{
   public class MapCreationService
   {
      private readonly IRandom _random;
      private readonly int _width;
      private readonly int _height;
      private readonly int _maxRooms;
      private readonly int _roomMaxSize;
      private readonly int _roomMinSize;
      private readonly int _level;
      private readonly DungeonMap _map;
      private readonly EquipmentCreationService _equipmentCreationService;

      public MapCreationService( int width, int height, int maxRooms, int roomMaxSize, int roomMinSize, int level, IRandom random )
      {
         _width = width;
         _height = height;
         _maxRooms = maxRooms;
         _roomMaxSize = roomMaxSize;
         _roomMinSize = roomMinSize;
         _level = level;
         _random = random;
         _map = new DungeonMap();
         _equipmentCreationService = new EquipmentCreationService( level );
      }

      public DungeonMap CreateMap()
      {
         _map.Initialize( _width, _height );

         for ( int r = 0; r < _maxRooms; r++ )
         {
            int roomWidth = _random.Next( _roomMinSize, _roomMaxSize );
            int roomHeight = _random.Next( _roomMinSize, _roomMaxSize );
            int roomXPosition = _random.Next( 0, _width - roomWidth - 1 );
            int roomYPosition = _random.Next( 0, _height - roomHeight - 1 );

            var newRoom = new Rectangle( roomXPosition, roomYPosition, roomWidth, roomHeight );
            bool newRoomIntersects = _map.Rooms.Any( room => newRoom.Intersects( room ) );
            if ( !newRoomIntersects )
            {
               _map.Rooms.Add( newRoom );
            }
         }

         foreach ( Rectangle room in _map.Rooms )
         {
            CreateMap( room );
         }

         for ( int r = 0; r < _map.Rooms.Count; r++ )
         {
            if ( r == 0 )
            {
               continue;
            }

            int previousRoomCenterX = _map.Rooms[r - 1].Center.X;
            int previousRoomCenterY = _map.Rooms[r - 1].Center.Y;
            int currentRoomCenterX = _map.Rooms[r].Center.X;
            int currentRoomCenterY = _map.Rooms[r].Center.Y;

            if ( _random.Next( 0, 2 ) == 0 )
            {
               CreateHorizontalTunnel( previousRoomCenterX, currentRoomCenterX, previousRoomCenterY );
               CreateVerticalTunnel( previousRoomCenterY, currentRoomCenterY, currentRoomCenterX );
            }
            else
            {
               CreateVerticalTunnel( previousRoomCenterY, currentRoomCenterY, previousRoomCenterX );
               CreateHorizontalTunnel( previousRoomCenterX, currentRoomCenterX, currentRoomCenterY );
            }
         }

         foreach ( Rectangle room in _map.Rooms )
         {
            CreateDoors( room );
         }

         CreateStairs();

         PlacePlayer();

         PlaceMonsters();

         PlaceEquipment();

         PlaceAbility();

         return _map;
      }

      private void CreateMap( Rectangle room )
      {
         for ( int x = room.Left + 1; x < room.Right; x++ )
         {
            for ( int y = room.Top + 1; y < room.Bottom; y++ )
            {
               _map.SetCellProperties( x, y, true, true );
            }
         }
      }

      private void CreateHorizontalTunnel( int xStart, int xEnd, int yPosition )
      {
         for ( int x = Math.Min( xStart, xEnd ); x <= Math.Max( xStart, xEnd ); x++ )
         {
            _map.SetCellProperties( x, yPosition, true, true );
         }
      }

      private void CreateVerticalTunnel( int yStart, int yEnd, int xPosition )
      {
         for ( int y = Math.Min( yStart, yEnd ); y <= Math.Max( yStart, yEnd ); y++ )
         {
            _map.SetCellProperties( xPosition, y, true, true );
         }
      }

      private void CreateDoors( Rectangle room )
      {
         int xMin = room.Left;
         int xMax = room.Right;
         int yMin = room.Top;
         int yMax = room.Bottom;

         List<Cell> borderCells = _map.GetCellsAlongLine( xMin, yMin, xMax, yMin ).ToList();
         borderCells.AddRange( _map.GetCellsAlongLine( xMin, yMin, xMin, yMax ) );
         borderCells.AddRange( _map.GetCellsAlongLine( xMin, yMax, xMax, yMax ) );
         borderCells.AddRange( _map.GetCellsAlongLine( xMax, yMin, xMax, yMax ) );

         foreach ( Cell cell in borderCells )
         {
            if ( IsPotentialDoor( cell ) )
            {
               _map.SetCellProperties( cell.X, cell.Y, false, true );
               _map.Doors.Add( new Door {
                  X = cell.X,
                  Y = cell.Y,
                  IsOpen = false
               } );
            }
         }
      }

      private bool IsPotentialDoor( Cell cell )
      {
         if ( !cell.IsWalkable )
         {
            return false;
         }

         Cell right = _map.GetCell( cell.X + 1, cell.Y );
         Cell left = _map.GetCell( cell.X - 1, cell.Y );
         Cell top = _map.GetCell( cell.X, cell.Y - 1 );
         Cell bottom = _map.GetCell( cell.X, cell.Y + 1 );

         if ( _map.GetDoor( cell.X, cell.Y ) != null ||
              _map.GetDoor( right.X, right.Y ) != null ||
              _map.GetDoor( left.X, left.Y ) != null ||
              _map.GetDoor( top.X, top.Y ) != null ||
              _map.GetDoor( bottom.X, bottom.Y ) != null )
         {
            return false;
         }

         if ( right.IsWalkable && left.IsWalkable && !top.IsWalkable && !bottom.IsWalkable )
         {
            return true;
         }
         if ( !right.IsWalkable && !left.IsWalkable && top.IsWalkable && bottom.IsWalkable )
         {
            return true;
         }
         return false;
      }

      private void CreateStairs()
      {
         _map.StairsUp = new Stairs {
            X = _map.Rooms.First().Center.X + 1,
            Y = _map.Rooms.First().Center.Y,
            IsUp = true
         };
         _map.StairsDown = new Stairs {
            X = _map.Rooms.Last().Center.X,
            Y = _map.Rooms.Last().Center.Y,
            IsUp = false
         };
      }

      private void PlaceMonsters()
      {
         foreach ( var room in _map.Rooms )
         {
            if ( Dice.Roll( "1D10" ) < 7 )
            {
               var numberOfMonsters = Dice.Roll( "1D4" );
               for ( int i = 0; i < numberOfMonsters; i++ )
               {
                  if ( DoesRoomHaveWalkableSpace( room ) )
                  {
                     Point randomRoomLocation = GetRandomLocationInRoom( room );
                     if ( randomRoomLocation != null )
                     {
                        _map.AddMonster( ActorCreationService.CreateMonster( _level, GetRandomLocationInRoom( room ) ) );
                     }
                  }
               }
            }
         }
      }

      private void PlaceEquipment()
      {
         foreach ( var room in _map.Rooms )
         {
            if ( Dice.Roll( "1D10" ) < 3 )
            {
               if ( DoesRoomHaveWalkableSpace( room ) )
               {
                  Point randomRoomLocation = GetRandomLocationInRoom( room );
                  if ( randomRoomLocation != null )
                  {
                     Equipment equipment;
                     try
                     {
                        equipment = _equipmentCreationService.CreateEquipment();
                     }
                     catch ( InvalidOperationException )
                     {
                        // no more equipment to generate so just quit adding to this level
                        return;
                     }
                     Point location = GetRandomLocationInRoom( room );
                     _map.AddEquipment( location.X, location.Y, equipment );
                  }
               }
            }
         }
      }

      private void PlacePlayer()
      {
         Player player = ActorCreationService.CreatePlayer();

         player.X = _map.Rooms[0].Center.X;
         player.Y = _map.Rooms[0].Center.Y;

         _map.AddPlayer( player );
      }

      private void PlaceAbility()
      {
         if ( _level == 1 || _level % 3 == 0 )
         {
            try
            {
               var ability = AbilityCreationService.CreateAbility();
               int roomIndex = _random.Next( 0, _map.Rooms.Count - 1 );
               Point location = GetRandomLocationInRoom( _map.Rooms[roomIndex] );
               _map.AddAbility( location.X, location.Y, ability );
            }
            catch ( InvalidOperationException )
            {
            }
         }
      }

      private Point GetRandomLocationInRoom( Rectangle room )
      {
         int x = _random.Next( 1, room.Width - 2 ) + room.X;
         int y = _random.Next( 1, room.Height - 2 ) + room.Y;
         if ( !_map.IsWalkable( x, y ) )
         {
            GetRandomLocationInRoom( room );
         }
         return new Point( x, y );
      }

      private bool DoesRoomHaveWalkableSpace( Rectangle room )
      {
         for ( int x = 1; x <= room.Width - 2; x++ )
         {
            for ( int y = 1; y <= room.Height - 2; y++ )
            {
               if ( _map.IsWalkable( x + room.X, y + room.Y ) )
               {
                  return true;
               }
            }
         }
         return false;
      }
   }
}