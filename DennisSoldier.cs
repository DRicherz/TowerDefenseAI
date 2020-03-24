using System;
using System.Collections.Generic;
using GameFramework;
using System.Xml;

namespace AI_Strategy
{
    //Basic node class with some added functionality
    //gCosts: Straight movement = 1, diagonal movement Sqrt(2)
    //Good practice: multiply by 10 -> straight = 10, diagonal = 14
    public class Node
    {
        
        public int xPos;
        public int yPos;
        public int hurtValue;      
        public int gCost;       // gCost = Distance to next node
        public int hCost;       // hCost = Distance to end node   

        public Node parent;
        public Node(int _xPos, int _yPos, int _hurtValue)
        {
            xPos = _xPos;
            yPos = _yPos;
            hurtValue = _hurtValue;
            
        }

        //Compare two nodes, check if xPosition and yPosition are equal, if they are, it is the same node
        public bool CompareNode(Node a, Node b)
        {
            
            if(a.xPos == b.xPos && a.yPos == b.yPos) return true;

            return false;
            
        }

        //fCost is gCost + hCost
        public int fCost
        {
            get { return gCost + hCost; }
            
        }
    }

    public class Grid
    {
        public Node[,] grid;

        //Create a new grid and assign the position and hurt values to the nodes
        public Node[,] CreateGrid(int[,] hurtMap)
        {         
            grid = new Node[PlayerLane.WIDTH, PlayerLane.HEIGHT];         
            for (int x = 0; x < PlayerLane.WIDTH; x++)
            {
                for (int y = 0; y < PlayerLane.HEIGHT; y++)
                {
                    grid[x, y] = new Node(x, y, hurtMap[x, y]);                                     
                }
            }
            return grid;
        }

        /* Get the neighbours of a node in a 3x3 grid
         * Example: N = Node, X = Neighbours
         * X-X-X
         * X-N-X
         * X-X-X         
         */
        public List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();
          
            for(int x = -1; x <= 1; x++)
            {
                for( int y = -1; y <= 1; y++)
                {
                    //Discard parameter node
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }
                    int tempX = node.xPos + x;
                    int tempY = node.yPos + y;
                    
                    //Prevent null exception
                    if (tempX >= 0 && tempX < PlayerLane.WIDTH && tempY >= 0 && tempY < PlayerLane.HEIGHT)
                    {                       
                        neighbours.Add(grid[tempX, tempY]);                       
                    }       
                }
            }
            return neighbours;
        }

        //Debug Method for Grid
        public void Printgrid(Grid grid) {

            for (int i = 0; i < PlayerLane.HEIGHT; i++)
            {
                Console.Write("\n");
                for (int k = 0; k < PlayerLane.WIDTH; k++)
                {
                     Console.Write("|" + "( " + grid.grid[k,i].xPos + "-" + grid.grid[k,i].yPos + ")" + "-" + grid.grid[k,i].gCost + "-" + grid.grid[k, i].hCost);        
                }
            }
                Console.ForegroundColor = ConsoleColor.White;          
        }
    }
    
    
    //Based on A* but weighted towards my hurt values 
    public class PathFinding
    {
       
        public Grid newGrid = new Grid();
        public List<Node> foundPath = new List<Node>();      
        
        public void FindPath(int startX, int startY, int targetX, int targetY, int[,] hurtMap)
        {
            newGrid.grid = newGrid.CreateGrid(hurtMap);
            
            Node startNode = new Node(startX, startY, hurtMap[startX, startY]);
            Node targetNode = new Node(targetX, targetY, hurtMap[targetX, targetY]);
            
            List<Node> openSet = new List<Node>();
            HashSet<Node> closedSet = new HashSet<Node>();

            openSet.Add(startNode);
            
            while (openSet.Count > 0)
            {   
                
                //Set the currentNode to the first node in the openSet
                Node currentNode = openSet[0];
                
                for (int i = 1; i < openSet.Count; i++)
                {                  
                    
                    if(openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost) 
                    {
                        if(openSet[i].hCost < currentNode.hCost )
                        {
                            currentNode = openSet[i];
                            
                        }
                    }
                }
                
                openSet.Remove(currentNode);
                closedSet.Add(currentNode);
                
                //If the curretNode is the targetNode a path has been found
                //Retrace it, see method
                if(currentNode.CompareNode(currentNode, targetNode))
                {                  
                    RetracePath(startNode, targetNode);
                    return;
                }
                
                foreach (Node neighbour in newGrid.GetNeighbours(currentNode))
                {
                    if (closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    //Heavily weighted towards the hurtValue
                    //Calculate the movement cost to the neighbour
                    int movementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + 5*currentNode.hurtValue;    
                    

                    if(movementCostToNeighbour < neighbour.gCost + 5 * currentNode.hurtValue || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = movementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);                      
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                        }
                        
                    }
                }     
            }          
        }


        //Trace back the chosen path by starting from the endnode
        //Adding the current node to a list and setting the current node ([0] = endnode) to the nextNode.parent 
        //Repeat until the start node is the current node
        //Reverse the list to get the path
        void RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();

            Node startingNode = newGrid.grid[startNode.xPos, startNode.yPos];
            Node node = newGrid.grid[endNode.xPos, endNode.yPos];

            while (node != startingNode && node != null)
            {               
                path.Add(node);
                node = node.parent;
            }

            path.Reverse();
            foundPath = path;
        }

        //Get the Distance between two nodes
        int GetDistance(Node nodeA, Node nodeB)
        {
            int distanceX = Math.Abs(nodeA.xPos - nodeB.xPos);
            int distanceY = Math.Abs(nodeA.yPos - nodeB.yPos);

            if (distanceX > distanceY) { return (14 * distanceY) + (10 * (distanceX - distanceY)); }
            else
            {
                return (14 * distanceX) + (10 * (distanceY - distanceX));
            }        
        }
       
    }
    
    /*
     * This class derives from Soldier and provides a new move method. Your assignment should
     * do the same - but with your own movement strategy.
     */
    public class DennisSoldier : Soldier
    {
        public int soldier_xPos;
        public int soldier_yPos;

        private TowerHurtMap unitHurtMap;
        PathFinding pathFinding = new PathFinding();
        private List<Node>[] allPaths= new List<Node>[7] ;
        

        public DennisSoldier(Player player, PlayerLane lane, int x) : base(player, lane, x)
        {

        }

        /*
          * This move method is a mere copy of the base movement method.
          */

        
        public override void move()
        {
            soldier_xPos = posX;
            soldier_yPos = posY;
            unitHurtMap = new TowerHurtMap(lane);
            unitHurtMap.enemyTowerHurtMap = unitHurtMap.GenerateHurtMap();

            //Check if the enemy lane has any towers, if so, alter the movement strategy to be most efficient
            if (DoesEnemyHaveTowers(unitHurtMap))
            {
                moveTo(posX, Math.Clamp(posY + 1, 0, PlayerLane.HEIGHT));
            }
            //Otherwise check for a few conditions: //TurnCounter.turnCounter < 250 && 
            //  1. There were no walls detected
            //  2. The strategy is considered efficient
            //  3. Or my backup strategy is not active
            //Then move according to weighted A* pathfinding
            else if (!EvaluateScore.backUpStrategy)
            {
                if (!unitHurtMap.DetectWalls(unitHurtMap.enemyTowerPositions) || EvaluateScore.efficientOne || EvaluateScore.efficientTwo)
                {

                    //XmlWriter xmlWriter = XmlWriter.Create("paths.xml");

                    //Because A* is usually used to get the path to 1 goal, I have to generate a path to each of the goals (X:0-6|Y:19) to find the best one

                    for (int i = 0; i < PlayerLane.WIDTH; i++)
                    {
                        pathFinding.FindPath(posX, posY, i, 19, unitHurtMap.enemyTowerHurtMap);
                        allPaths[i] = pathFinding.foundPath;

                    }
                    //WritePathsToXML(xmlWriter);

                    //After that, calculate which path results in the last damage taken
                    pathFinding.foundPath = EvaluatePath();
                    //If a path was found, remove the first node (unit's current position) and move towards the next node
                    if (pathFinding.foundPath.Count > 0)
                    {
                        pathFinding.foundPath.RemoveAt(0);
                        moveTo(pathFinding.foundPath[0].xPos, pathFinding.foundPath[0].yPos);
                    }
                }
            }              
            //In case my backup strategy is active
            else
            {
                moveTo(posX, Math.Clamp(posY + 1, 0, PlayerLane.HEIGHT));
            }
            

        }

        //Debug method to create an XML file of the paths
        private void WritePathsToXML(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartDocument();

                xmlWriter.WriteStartElement("Paths");
                for (int i = 0; i < allPaths.Length; i++)
                {
                    xmlWriter.WriteStartElement("Paths" + i);
                    
                    foreach (Node n in allPaths[i])
                    {
                        xmlWriter.WriteString(n.xPos + "|" + n.yPos + " - ");

                    }
                    
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        //Evaluate the path with the least amount of damage taken
        //Because we got 7 potential goals (X=0-6|Y=19), it is important to evaluate each goal
        private List<Node> EvaluatePath()
        {
            
            List<Node> bestPath = new List<Node>();
            int[] pathHurtValues = new int[7];

            for(int i = 0; i < allPaths.Length; i++)
            {
                int hurtValue = new int();
                   
                foreach (Node node in allPaths[i])
                {
                    hurtValue += node.hurtValue;
                }
                pathHurtValues[i] = hurtValue;
                
            }
            int tempHurt = 100;
            for (int i = 6; i != 0 ; i--)
            {
                
                if(pathHurtValues[i] < tempHurt)
                {
                    tempHurt = pathHurtValues[i];
                    bestPath = allPaths[i];
                }
            }
            return bestPath;
        }
        
        //Check if the enemy has tower on his defendlane
        private bool DoesEnemyHaveTowers(TowerHurtMap hurtMap)
        {
            int hurt = hurtMap.ReturnWholeHurtMapValue(hurtMap.enemyTowerHurtMap);
            
            if(hurt == 0) return true;
            
                return false;


        }

    }
}
