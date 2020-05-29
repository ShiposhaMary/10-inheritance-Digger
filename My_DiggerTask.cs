using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digger
/* Каждый элемент должен уметь:

Возвращать имя файла, в котором лежит соответствующая ему картинка 
(например, "Terrain.png")
Сообщать приоритет отрисовки. 
Чем выше приоритет, тем раньше рисуется соответствующий элемент, 
это важно для анимации.
Действовать — возвращать направление перемещения и,
если объект во что-то превращается на следующем ходу, 
то результат превращения.
Разрешать столкновения двух элементов в одной клетке.
Сделайте классы Sack,Gold, Monster, реализовав ICreature. 
Его поведение должно быть таким:

Если на карте нет диггера, монстр стоит на месте.
Если на карте есть диггер, монстр двигается в его сторону по горизонтали
или вертикали. Можете написать поиск кратчайшего пути к диггеру, 
но это не обязательно.
Монстр не может ходить сквозь землю или мешки.
Если после хода монстр и диггер оказались в одной клетке, диггер умирает.
Если монстр оказывается в клетке с золотом, золото исчезает.
Мешок может лежать на монстре.
Падающий на монстра мешок убивает монстра.
Монстр не должен начинать ходить в клетку, где уже есть другой монстр.
Если два или более монстров сходили в одну и ту же клетку, они все умирают.
Если в этой клетке был диггер — он тоже умирает.

*/
{
    public class Terrain : ICreature
    {
        public string GetImageFileName() => "Terrain.png";
        public int GetDrawingPriority() => 999;
        public CreatureCommand Act(int x, int y) =>
            new CreatureCommand { DeltaX = 0, DeltaY = 0, TransformTo = this };
        public bool DeadInConflict(ICreature conflictedObject) =>
            conflictedObject is Player;
    }

    public class Player : ICreature
    {
        public string GetImageFileName() => "Digger.png";

        public int GetDrawingPriority() => 0;

        public CreatureCommand Act(int x, int y)
        {
            var command = new CreatureCommand { DeltaX = 0, DeltaY = 0, TransformTo = this };
            switch (Game.KeyPressed)
            {
                case System.Windows.Forms.Keys.Up:
                    command.DeltaY = -1;
                    break;
                case System.Windows.Forms.Keys.Down:
                    command.DeltaY = 1;
                    break;
                case System.Windows.Forms.Keys.Left:
                    command.DeltaX = -1;
                    break;
                case System.Windows.Forms.Keys.Right:
                    command.DeltaX = 1;
                    break;
                default:
                    break;
            }
            if (!CanWalkTo(x + command.DeltaX, y + command.DeltaY)) command.DeltaX = command.DeltaY = 0;
            else if (Game.Map.GetValue(x + command.DeltaX, y + command.DeltaY) is Gold) Game.Scores += 10;
            return command;
        }

        public bool DeadInConflict(ICreature conflictedObject) =>
            (conflictedObject is Sack) ||
            (conflictedObject is Monster);

        private bool CanWalkTo(int x, int y)
        {
            if (x < 0 || y < 0 || Game.MapWidth <= x || Game.MapHeight <= y) return false;
            var cell = Game.Map.GetValue(x, y);
            return !(cell is Sack);
        }
    }

    public class Sack : ICreature
    {
        public string GetImageFileName() => "Sack.png";
        public int GetDrawingPriority() => 10;

        public CreatureCommand Act(int x, int y)
        {
            var cmd = new CreatureCommand { DeltaX = 0, DeltaY = 1, TransformTo = this };

            if (CanFallTo(x + cmd.DeltaX, y + cmd.DeltaY))
            {
                ++FallingTime;
            }
            else
            {
                if (FallingTime > 1)
                {
                    cmd.TransformTo = new Gold();
                }
                FallingTime = 0;
                cmd.DeltaY = 0;
            }
            return cmd;
        }

        private bool CanFallTo(int x, int y)
        {
            if (x < 0 || y < 0 || Game.MapWidth <= x || Game.MapHeight <= y)
                return false;
            var cell = Game.Map.GetValue(x, y);
            return (cell == null) ||
                (IsFalling() && ((cell is Player) || (cell is Monster)));
        }

        public bool DeadInConflict(ICreature conflictedObject) => false;

        public bool IsFalling() => FallingTime > 0;

        int FallingTime = 0;
    }

    public class Gold : ICreature
    {
        public string GetImageFileName() => "Gold.png";
        public int GetDrawingPriority() => 10;
        public CreatureCommand Act(int x, int y) =>
            new CreatureCommand { DeltaX = 0, DeltaY = 0, TransformTo = this };
        public bool DeadInConflict(ICreature conflictedObject) => true;
    }

    public class Monster : ICreature
    {
        public string GetImageFileName() => "Monster.png";

        public int GetDrawingPriority() => 20;

        public CreatureCommand Act(int x, int y)
        {
            var cmd = new CreatureCommand { DeltaX = 0, DeltaY = 0, TransformTo = this };
            if (IsPlayerInSection(0, 0, x, Game.MapHeight) && CanWalkTo(x - 1, y))
                cmd.DeltaX = -1;
            else if (IsPlayerInSection(x + 1, 0, Game.MapWidth, Game.MapHeight) &&
                     CanWalkTo(x + 1, y))
                cmd.DeltaX = 1;
            else if (IsPlayerInSection(0, 0, Game.MapWidth, y) && CanWalkTo(x, y - 1))
                cmd.DeltaY = -1;
            else if (IsPlayerInSection(0, y + 1, Game.MapWidth, Game.MapHeight) &&
                     CanWalkTo(x, y + 1))
                cmd.DeltaY = 1;
            return cmd;
        }

        private bool IsPlayerInSection(int x0, int y0, int x1, int y1)
        {
            for (var x = x0; x < x1; ++x)
            {
                for (var y = y0; y < y1; ++y)
                {
                    if (Game.Map.GetValue(x, y) is Player) return true;
                }
            }
            return false;
        }

        private bool CanWalkTo(int x, int y)
        {
            if (x < 0 || y < 0 || Game.MapWidth <= x || Game.MapHeight <= y) return false;
            var cell = Game.Map.GetValue(x, y);
            return (cell == null) ||
                !((cell is Sack) || (cell is Monster) || (cell is Terrain));
        }

        public bool DeadInConflict(ICreature conflictedObject) =>
            (conflictedObject is Monster) ||
            ((conflictedObject is Sack) && (conflictedObject as Sack).IsFalling());
    }

}
         
  
