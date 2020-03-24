using System;
using System.Collections.Generic;
using System.Text;
using GameFramework;
using System.Linq;

namespace AI_Strategy
{

    //Basic struct containing information about a tower and its position
    public struct EnemyTurrets
    {
        public Unit tower;
        public int posX;
        public int posY;

        public EnemyTurrets(Unit cellTower, int positionX, int positionY) 
        {
            this.tower = cellTower;
            this.posX = positionX;
            this.posY = positionY;
        }
    }

    //Is responsible for creating a hurtmap to indicate how much damage a soldier could take if he would move to a certain field
    //Crucial to my A* pathfinding
    public class TowerHurtMap
    {

        PlayerLane attackLane;
        private List<EnemyTurrets> enemyTowerStructList = new List<EnemyTurrets>();

        public int[,] enemyTowerPositions = new int[PlayerLane.WIDTH, PlayerLane.HEIGHT];
        public int[,] enemyTowerHurtMap = new int[PlayerLane.WIDTH, PlayerLane.HEIGHT];

        public TowerHurtMap(PlayerLane laneToGenerate)
        {
            this.attackLane = laneToGenerate;

        }

        //Generates a hurtmap of the enemy defendlane
        //Assign towers as values of 1 to an multidimensional array, which contains the whole enemy field       
        //Assigning value of +=2 to positions (of multidimensional array), where the range of the tower can hit units -> Generates a hurtmap      
        
        public int[,] GenerateHurtMap()
        {
            //Because we have no way of knowing if a turret has died, I have to reset the whole enemyTurret list each iteration
            enemyTowerStructList.Clear();

            //Generate the tower map
            for (int i = 0; i < PlayerLane.HEIGHT; i++)
            {
                for (int k = 0; k < PlayerLane.WIDTH; k++)
                {
                    Cell cell = attackLane.GetCellAt(k, i);

                    if (cell.Unit != null && cell.Unit.GetType() == typeof(Tower))
                    {
                        enemyTowerPositions[k, i] = 1;

                        //If a tower was detected, create a new EnemyTurrets instance and add it to the list                      
                        EnemyTurrets tempEnemyTurrets = new EnemyTurrets(cell.Unit, k, i);
                        if (!enemyTowerStructList.Contains(tempEnemyTurrets))
                        {
                            enemyTowerStructList.Add(tempEnemyTurrets);
                        }

                    }
                    else
                    {
                        enemyTowerPositions[k, i] = 0;
                    }

                }
            }

            /*Generate hurt map by increasing the values around a tower in a 5x5 grid by 2 for each tower
             * Example: T = Tower
             * 
             * 2-2-2-2-4-2-2-2-2
             * 2-2-2-2-4-2-2-2-2
             * 2-2-T-2-4-2-T-2-2
             * 2-2-2-2-4-2-2-2-2
             * 2-2-2-2-4-2-2-2-2
             */
            foreach (EnemyTurrets eT in enemyTowerStructList)
            {
                for (int x = Math.Clamp(eT.posX - 2, 0, PlayerLane.WIDTH - 1); x <= Math.Clamp(eT.posX + 2, 0, PlayerLane.WIDTH - 1); x++)
                {
                    for (int y = Math.Clamp(eT.posY - 2, 0, PlayerLane.HEIGHT - 1); y <= Math.Clamp(eT.posY + 2, 0, PlayerLane.HEIGHT - 1); y++)
                    {
                        enemyTowerHurtMap[x, y] += 2;
                    }
                }
            }
            return enemyTowerHurtMap;
        }
        
        //Debug method to print turrets
        public void PrintTurrets(int[,] arrayToPrint)
        {
            for (int i = 0; i < PlayerLane.HEIGHT; i++)
            {
                Console.Write("\n");
                for (int k = 0; k < PlayerLane.WIDTH; k++)
                {
                    if (arrayToPrint[k, i] == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("|" + arrayToPrint[k, i]);
                    }
                    else if (arrayToPrint[k, i] == 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("|" + arrayToPrint[k, i]);
                    }
                    else if (arrayToPrint[k, i] > 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("|" + arrayToPrint[k, i]);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("|" + arrayToPrint[k, i]);
                    }


                }
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        //Returns the hurt value of the enemy lane
        public int ReturnWholeHurtMapValue(int[,] hurtMap)
        {
            int wholeHurt = 0;
            for(int i = 0; i < PlayerLane.WIDTH; i++)
            {
                for(int k = 0; k < PlayerLane.HEIGHT; k++)
                {
                    wholeHurt += hurtMap[i, k];
                }
            }
            return wholeHurt;
        }

        //Returns true if a wall was detected, otherwise false
        public bool DetectWalls(int [,] hurtMap)
        {           
            for(int y = 0; y < PlayerLane.HEIGHT; y++)
            {                           
                if(hurtMap[0,y] == 1 && hurtMap[1, y] == 1 && hurtMap[2, y] == 1 && hurtMap[3, y] == 1 && hurtMap[4, y] == 1 && hurtMap[5, y] == 1 && hurtMap[6, y] == 1)
                {
                    //Console.WriteLine("Walls detected");
                    
                    return true;
                }
            }
            //Console.WriteLine("No Walls detected");
            return false;
        }
    }

    //Personal TurnCounter to keep track of current turn. Is necessary due to the protection level of TowerDefense.turns
    public static class TurnCounter
    {
        public static int turnCounter = 0;
        public static void IncrementTurn() { turnCounter++; }
    }

    //Evaluates if my score has increased each x turns
    //Also has some variables that are used by my soldiers (efficientOne/Two, backUpStrategy)
    //Therefore public static
    public static class EvaluateScore
    {
        public static int previousScore{ get; set; }
        public static bool efficientOne { get; set; }
        public static bool efficientTwo { get; set; }
        public static bool backUpStrategy { get; set; }
        public static void SetBools()
        {
            efficientOne = true;
            efficientTwo = true;
            backUpStrategy = false;
        }
        
        public static bool Scoring(Player player)
        { 
            if (TurnCounter.turnCounter > 90)
            {

                //Check if my score increased since the previous score was stored
                if (player.Score - previousScore >= 3)
                {
                    return true;

                }
                 return false;
              
            }
            return true;
        }
}

public class DennisStrategy : AbstractStrategy
    {
        private bool defenseBreached = false;
        bool setUpBools = false;

        TowerHurtMap hurtMap;       

        public DennisStrategy(PlayerLane defendLane, PlayerLane attackLane, Player player) : base(defendLane, attackLane, player)
        {          
            this.defendLane = defendLane;
            this.attackLane = attackLane;
            this.player = player;
            
        }
        
        
        public override void DeployTowers()
        {
            //Start my own TurnCounter
            TurnCounter.IncrementTurn();

            //Only want the efficient bools to be set once
            if (!setUpBools)
            {
                setUpBools = true;
                EvaluateScore.SetBools();
            }
            //Check for breached units = behind my first towerlane
            List<Unit> breachedUnits = new List<Unit>();

            for(int i = 0; i < PlayerLane.WIDTH; i++)
            {                              
                for(int k = 2; k < PlayerLane.HEIGHT; k++)
                {
                    Cell cell = defendLane.GetCellAt(i, k);
                    if (cell.Unit != null && cell.Unit.GetType() == typeof(Soldier))
                    {                        
                        breachedUnits.Add(cell.Unit);
                    }
                }
            }
           
            // If more than two lanes full of soldiers get through my initial defense
            // Consider my defense as breached and fall back to second defense line
            if(breachedUnits.Count >= 14)
            {
                defenseBreached = true;
            }

            //Generate a heatmap for later use
            hurtMap = new TowerHurtMap(attackLane);
            hurtMap.enemyTowerHurtMap = hurtMap.GenerateHurtMap();

            //Personally I think this is the best tower placement: Counters f.e. pooling soldiers out of range of my towers and engaging then
            //Therefore this is rather static
            if (!defenseBreached)
            {
                for (int i = 0; i < 3; i++)
                {
                    Tower tower = player.BuyTower(defendLane, i, 2);
                }
                for (int i = 4; i < 7; i++)
                {
                    Tower newTower = player.BuyTower(defendLane, i, 2);
                }
                for (int i = 1; i < 7; i += 2)
                {
                    Tower newerTower = player.BuyTower(defendLane, i, 3);
                }
            }
            else
            {
                for (int j = PlayerLane.HEIGHT-2; j <= PlayerLane.HEIGHT; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Tower tower = player.BuyTower(defendLane, i, j);
                    }
                    for (int i = 6; i > 2; i--)
                    {
                        Tower newTower = player.BuyTower(defendLane, i, j);
                    }
                    
                }
                
            }

        }

        public override void DeploySoldiers()
        {
            //GetOwnSoldiers();
            //This is the basis of my strategy evaluation
            //If the score is rising, the strategy works and Soldiers score
            //If not, I switch to my backup strategy
            //Evaluation starts after 100 turns because I have to create towers myself

            //Every 14 turns, check for progress in the score department           
            if (TurnCounter.turnCounter % 14 == 0)
            {
                if (EvaluateScore.efficientOne)
                {
                    EvaluateScore.efficientOne = EvaluateScore.Scoring(player);
                }
                else
                {
                    EvaluateScore.efficientTwo = EvaluateScore.Scoring(player);
                }
            }

            //Every 10 turns set the current score as previous score 
            if (TurnCounter.turnCounter % 10  == 0)
            {                
                //Console.WriteLine(EvaluateScore.previousScore);
                EvaluateScore.previousScore = player.Score;
            }

            //---Strategy A--- Strategy versus non-wall using enemies
            //If there are no towers on the map, or my soldier score and no walls detected 
            //Spawn waves of soldiers every 14 gold
            hurtMap.GenerateHurtMap();
            if (!hurtMap.DetectWalls(hurtMap.enemyTowerPositions) && !EvaluateScore.backUpStrategy)
            {
                if (hurtMap.ReturnWholeHurtMapValue(hurtMap.enemyTowerHurtMap) == 0 || EvaluateScore.efficientOne || EvaluateScore.efficientTwo )
                {
                    if (player.Gold > 14)
                    {
                        for (int i = 0; i < PlayerLane.WIDTH; i++)
                        {
                            Soldier soldier = this.player.BuySoldier(attackLane, i);

                        }
                    }
                }
            }           
            //---Strategy B--- Strategy against enemies that use a (double) wall to pool money and spam soldiers at turn >900
            //If my first strategy is not succesful and my score does not increase
            //Pool money until turn 750, then use that money to diminish all the saved money of the enemy, destroy his base and win the game
            else
            {
                EvaluateScore.backUpStrategy = true;
                if (TurnCounter.turnCounter > 750)
                {                       
                    for (int i = 0; i < PlayerLane.WIDTH; i++)
                    {
                        Soldier soldier = this.player.BuySoldier(attackLane, i);
                        
                    } 
                }
            }
            
        }

        public override List<Soldier> SortedSoldierArray(List<Soldier> unsortedList)
        {        
            //foreach(DennisSoldier ds in unsortedList)
            //{
            //    unsortedList.OrderBy(DennisSoldier => ds.soldier_yPos).ThenBy(DennisSoldier => ds.soldier_yPos);
            //}

            return unsortedList;
        }    
    }
}
