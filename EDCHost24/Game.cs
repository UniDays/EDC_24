using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Security.Cryptography;
using System.Diagnostics;
using Dot = EDCHOST24.Dot;
using Car = EDCHOST24.Car;
using Package = EDCHOST24.Package;
using PackageList = EDCHOST24.PackageList;
using Station = EDCHOST24.Station;
using Labyrinth = EDCHOST24.Labyrinth;
using Boundary = EDCHOST24.Boundary;

namespace EDCHOST24
{
    // Token
    // 当前游戏状态
    public enum GameState { UNSTART = 0, RUN = 1, PAUSE = 2, END = 3};
    // 游戏阶段

    public enum GameStage { NONE = 0, FIRST_HALF = 1,  SENCOND_HALF= 2};

    public class Game
    {
        
        // size of competition area
        // 最大游戏场地
        public const int MAX_SIZE = 254;
        public const int AVAILIABLE_MAX_X = 254;
        public const int AVAILIABLE_MIN_X = 0;
        public const int AVAILIABLE_MAX_Y = 254;
        public const int AVAILIABLE_MIN_Y = 0;
        public const int EDGE_DISTANCE = 28;
        public const int ENTRANCE_WIDTH = 36;
        public const int LINE_WIDTH = 2;

        // size of car
        public const int COLLISION_RADIUS = 8;

        // initial amount of package
        public const int INITIAL_PKG_NUM = 5;
        // time interval of packages
        public const int TIME_INTERVAL = 1500;

        // time of first and second half
        public const int FIRST_HALF_TIME = 60000;
        public const int SECOND_HALF_TIME = 180000;

        // Message Token
        public const byte START = 0xff;
        public const byte END = 0;

        // state
        public GameStage mGameStage;
        public GameState mGameState;

        // Time
        // Set time zero as the start time of each race
        private int mStartTime; // system time, update for each race
        private int mGameTime;
        private int mTimeRemain;

        // car and package
        private Car mCarA, mCarB;

        private int[] mScoreA, mScoreB;

        private PackageList mPackageFirstHalf;
        private PackageList mPackageSecondHalf;

        // store the packages on the field
        private List<Package> mPackagesRemain;

        // Charge station set by Team A and B
        private Station mChargeStation;

        // which team is racing A or B
        private Camp mCamp;

        // obstacle
        private Labyrinth mObstacle;

        private Boundary mBoundary;

        // flags represents whether package list has been generated
        private bool hasFirstPackageListGenerated;
        private bool hasSecondPackageListGenerated;
    

        /***********************************************************************
        Interface used for Tracker to display the information of current game
        ***********************************************************************/
        public Game()
        {
            Debug.WriteLine("Call Constructor of Game");

            mGameState = GameState.UNSTART;
            mGameStage = GameStage.NONE;

            mCamp = Camp.NONE;

            mCarA = new Car(Camp.A, 0);
            mCarB = new Car(Camp.B, 0);

            mScoreA = new int() {0,0};
            mScoreB = new int() {0,0};

            hasFirstPackageListGenerated = false;
            hasSecondPackageListGenerated = false;

            mChargeStation = new Station();

            mPackagesRemain = new List<Package> ();

            mStartTime = -1;
            mGameTime = -1;
            mTimeRemain = 0;

            mObstacle = new Labyrinth();
            mBoundary = new Boundary(MAX_SIZE, EDGE_DISTANCE, ENTRANCE_WIDTH, LINE_WIDTH);
        }


        /***********************************************
        Update on each frame
        ***********************************************/
        public void UpdateOnEachFrame(Dot _CarPos)
        {
            // check the game state
            if (mGameState == GameState.UNSTART)
            {
                Debug.WriteLine("Failed to update on frame! The game state is unstart.");
                return;
            } 
            else if (mGameState = GameState.PAUSE)
            {
                Debug.WriteLine("Failed to update on frame! The game state is pause.");
                return;
            }
            else if (mGameState == GameState.END)
            {
               Debug.WriteLine("Failed to update on frame! The game state is end.");
               return;
            }

            if (mCamp == Camp.NONE)
            {
                Debug.WriteLine("Failed to update on frame! Camp is none which expects to be A or B");
                return;
            }

            _UpdateGameTime();

            // Try to generate packages on each refresh
            _GeneratePackage();

            int TimePenalty = 0;

            // Update car's info on each frame
            if (mCamp == Camp.A)
            {
                mCarA.Update(_CarPos, mGameTime, _IsOnBlackLine(_CarPos), 
                _IsInObstacle(_CarPos), _IsInOpponentStation(_CarPos), 
                _IsInChargeStation(_CarPos), mPackagesRemain, TimePenalty);
            }
            else if (mCamp == Camp.B)
            {
                mCarB.Update(_CarPos, mGameTime, _IsOnBlackLine(_CarPos), 
                _IsInObstacle(_CarPos), _IsInOpponentStation(_CarPos), 
                _IsInChargeStation(_CarPos), mPackagesRemain, TimePenalty);
            }

            //update times remain
            mTimeRemain = mTimeRemain - mGameTime - TimePenalty;

            //judge wether to end the game automatiacally
            if (mTimeRemain <= 0)
            {
                mGameState = GameState.END;
                Debug.WriteLine("Time remaining is up to 0. The End.");
            }
        }

        public void SetChargeStation()
        {
            if (mCamp == Camp.A)
            {
                mCarA.SetChargeStation();
                mChargeStation.Add(mCarA.CurrentPos(), 1);
            }
            else if (mCamp == Camp.B)
            {
                mCarB.SetChargeStation();
                mChargeStation.Add(mCarB.CurrentPos(), 2);
            }
        }

        public void GetMark()
        {
            if (mCamp == Camp.A)
            {
                mCarA.GetMark();
            }
            else if (mCamp == Camp.B)
            {
                 mCarB.GetMark();
            }
        }

        // decide which team and stage is going on
        public void Start (Camp _camp, GameStage _GameStage)
        {
            if (mGameState == GameState.RUN)
            {
                Debug.WriteLine("Failed! The current game is going on");
                return;
            }

            if (_GameStage != GameStage.FIRST_HALF && _GameStage != GameStage.SENCOND_HALF)
            {
                Debug.WriteLine("Failed to set game stage! Expect input to be GameStage.FIRST_HALF or GameStage.SECOND_HALF");
            }

            // Generate the package list
            if (!hasFirstPackageListGenerated && mGameStage == GameStage.FIRST_HALF)
            {
                mPackageFirstHalf = mPackageFirstHalf = new PackageList(AVAILIABLE_MAX_X, AVAILIABLE_MIN_X, 
                        AVAILIABLE_MAX_Y, AVAILIABLE_MIN_Y, INITIAL_PKG_NUM, FIRST_HALF_TIME, TIME_INTERVAL, 0);
            }

            if (!hasSecondPackageListGenerated && mGameStage == GameStage.SENCOND_HALF)
            {
                mPackageFirstHalf = mPackageFirstHalf = new PackageList(AVAILIABLE_MAX_X, AVAILIABLE_MIN_X, 
                        AVAILIABLE_MAX_Y, AVAILIABLE_MIN_Y, INITIAL_PKG_NUM, FIRST_HALF_TIME, TIME_INTERVAL, 1);
            }

            // set state param of game
            mGameState =  GameState.RUN;
            mGameStage = _GameStage;
            mCamp = _camp;

            if (mCamp == Camp.A)
            {
                mScoreA[(int)mGameStage - 1] = 0;
            }
            else if (mCamp == Camp.B)
            {
                mScoreB[(int)mGameStage - 1] = 0;
            }

            // initial packages on the field
            _InitialPackagesRemain();

            if (mGameStage = GameStage.FIRST_HALF)
            {
                mTimeRemain = FIRST_HALF_TIME;
            }
            else if (mGameStage = GameStage.SENCOND_HALF)
            {
                mTimeRemain = SECOND_HALF_TIME;
            }

            mStartTime = _GetCurrentTime();
            mGameTime = 0;
        }

        public void Pause()
        {
            if (mGameState != GameState.RUN)
            {
                Debug.WriteLine("Pause failed! No race is going on.");
                return;
            }
            mGameState = GameState.PAUSE;
        }

        public void Continue()
        {
            if (mGameState != GameState.PAUSE)
            {
                Debug.WriteLine("Continue Failed! No race is suspended");
            }
            mGameState = GameState.RUN;
        }

        // finish on a manul mode
        public void End ()
        {
            if (mGameState != GameState.RUN)
            {
                Debug.WriteLine("Failed! There is no game going on");
            }

            //Reset Car and Save Score
            if (mCamp == Camp.A)
            {
                mScoreA[(int)mGameStage - 1] = mCarA.GetScore();
                mCarA.Reset();
            } 
            else if (mCamp == Camp.B)
            {
                mScoreB[(int)mGameStage - 1] = mCarB.GetScore();
                mCarB.Reset();
            }
            
            // Reset pointer which used to genrate packages
            if (mGameStage ==  GameStage.FIRST_HALF)
            {
                mPackageFirstHalf.ResetPointer();
            }
            else if (mGameStage ==  GameStage.SENCOND_HALF)
            {
                mPackageSecondHalf.ResetPointer();
            }

            // set state param of game
            mGameState = GameState.UNSTART;
            mGameStage = GameStage.NONE;
            mCamp = Camp.NONE;

            mPackagesRemain.Clear();

            mStartTime = -1;
            mGameTime = -1;
        }

        public byte[] Message()
        {
            byte[] MyMessage = new byte[100];
            int Index = 0;
            // Game Stage
            MyMessage[Index++] = (byte) mGameStage;
            // Game State
            MyMessage[Index++] = (byte) mGameState;

            // GameTime 
            MyMessage[Index++] = (byte) ((mGameTime/100) >> 8);
            MyMessage[Index++] = (byte) (mGameTime/100);

            // TimeRemain
            MyMessage[Index++] = (byte) ((mTimeRemain/100) >> 8);
            MyMessage[Index++] = (byte) (mTimeRemain/100);


            // Obstacle
            // Add your code here...
            foreach(Wall item in mObstacle.mpWallList)
            {
                MyMessage[Index++] = (byte) (item.w1.x);
                MyMessage[Index++] = (byte) (item.w1.y);
                MyMessage[Index++] = (byte) (item.w2.x);
                MyMessage[Index++] = (byte) (item.w2.x);
            }

            if (mCamp == Camp.A)
            {
                // Your Charge Station
                MyMessage[Index++] = (byte) mChargeStation.Index(0, 0).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(0, 0).y;
                MyMessage[Index++] = (byte) mChargeStation.Index(1, 0).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(1, 0).y;
                MyMessage[Index++] = (byte) mChargeStation.Index(2, 0).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(2, 0).y;

                // Oppenont's Charge Station
                MyMessage[Index++] = (byte) mChargeStation.Index(0, 1).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(0, 1).y;
                MyMessage[Index++] = (byte) mChargeStation.Index(1, 1).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(1, 1).y;
                MyMessage[Index++] = (byte) mChargeStation.Index(2, 1).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(2, 1).y;

                // Score
                MyMessage[Index++] = (byte) (mCarA.GetScore() >> 8);
                MyMessage[Index++] = (byte) (mCarA.GetScore());

                // Car Position
                MyMessage[Index++] = (byte) (mCarA.CurrentPos().x);
                MyMessage[Index++] = (byte) (mCarA.CurrentPos().y);

                // Car's Package List
                MyMessage[Index++] = (byte) (mCarA.GetPackageCount());
                // Destinaton, Scheduled Time
                for (int i = 0;i < 5;i++)
                {
                    MyMessage[Index++] = (byte) (mCarA.GetPackageOnCar(i).Destination().x);
                    MyMessage[Index++] = (byte) (mCarA.GetPackageOnCar(i).Destination().y);
                    MyMessage[Index++] = (byte) (mCarA.GetPackageOnCar(i).ScheduledDeliveryTime()/100 >> 8);
                    MyMessage[Index++] = (byte) (mCarA.GetPackageOnCar(i).ScheduledDeliveryTime());
                }
            }
            else if (mCamp == Camp.B)
            {
                // Your Charge Station
                MyMessage[Index++] = (byte) mChargeStation.Index(0, 1).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(0, 1).y;
                MyMessage[Index++] = (byte) mChargeStation.Index(1, 1).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(1, 1).y;
                MyMessage[Index++] = (byte) mChargeStation.Index(2, 1).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(2, 1).y;

                // Oppenont's Charge Station
                MyMessage[Index++] = (byte) mChargeStation.Index(0, 0).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(0, 0).y;
                MyMessage[Index++] = (byte) mChargeStation.Index(1, 0).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(1, 0).y;
                MyMessage[Index++] = (byte) mChargeStation.Index(2, 0).x;
                MyMessage[Index++] = (byte) mChargeStation.Index(2, 0).y;

                // Score
                MyMessage[Index++] = (byte) (mCarB.GetScore() >> 8);
                MyMessage[Index++] = (byte) (mCarB.GetScore());

                // Car Position
                MyMessage[Index++] = (byte) (mCarB.CurrentPos().x);
                MyMessage[Index++] = (byte) (mCarB.CurrentPos().y);

                // Car's Package List
                // Total numbrt of picked packages
                MyMessage[Index++] = (byte) (mCarB.GetPackageCount());
                // Destinaton, Scheduled Time
                for (int i = 0;i < 5;i++)
                {
                    MyMessage[Index++] = (byte) (mCarB.GetPackageOnCar(i).Destination().x);
                    MyMessage[Index++] = (byte) (mCarB.GetPackageOnCar(i).Destination().y);
                    MyMessage[Index++] = (byte) (mCarB.GetPackageOnCar(i).ScheduledDeliveryTime()/100 >> 8);
                    MyMessage[Index++] = (byte) (mCarB.GetPackageOnCar(i).ScheduledDeliveryTime());
                }
            }

            // Packages Generate on this frame
            // Indentity Code, Departure, Destination, Scheduled Time
            if (mGameStage == GameStage.FIRST_HALF) 
            {
                MyMessage[Index++] = (byte) (mPackageFirstHalf.LastGenerationPackage().IndentityCode());
                MyMessage[Index++] = (byte) (mPackageFirstHalf.LastGenerationPackage().Departure().x);
                MyMessage[Index++] = (byte) (mPackageFirstHalf.LastGenerationPackage().Departure().y);
                MyMessage[Index++] = (byte) (mPackageFirstHalf.LastGenerationPackage().Destination().x);
                MyMessage[Index++] = (byte) (mPackageFirstHalf.LastGenerationPackage().Destination().y);
                MyMessage[Index++] = (byte) (mPackageFirstHalf.LastGenerationPackage().ScheduledDeliveryTime()/100 >> 8);
                MyMessage[Index++] = (byte) (mPackageFirstHalf.LastGenerationPackage().ScheduledDeliveryTime()/100);
            }
            else
            {
                MyMessage[Index++] = (byte) (mPackageSecondHalf.LastGenerationPackage().IndentityCode());
                MyMessage[Index++] = (byte) (mPackageSecondHalf.LastGenerationPackage().Departure().x);
                MyMessage[Index++] = (byte) (mPackageSecondHalf.LastGenerationPackage().Departure().y);
                MyMessage[Index++] = (byte) (mPackageSecondHalf.LastGenerationPackage().Destination().x);
                MyMessage[Index++] = (byte) (mPackageSecondHalf.LastGenerationPackage().Destination().y);
                MyMessage[Index++] = (byte) (mPackageSecondHalf.LastGenerationPackage().ScheduledDeliveryTime()/100 >> 8);
                MyMessage[Index++] = (byte) (mPackageSecondHalf.LastGenerationPackage().ScheduledDeliveryTime()/100);
            }

            return MyMessage;
        }

        /***********************************************************************
        Interface used for Tracker to display the information of current game
        ***********************************************************************/
        public List<Package> PackagesOnStage()
        {
            return mPackagesRemain;
        }

        public Camp GetCamp()
        {
            return mCamp;
        }
        

        /***********************************************************************
        Private Functions
        ***********************************************************************/

        /***********************************************
        Initialize and Generate Package
        ***********************************************/
        private bool _InitialPackagesRemain()
        {
            mPackagesRemain.Clear();

            if (mGameStage == GameStage.FIRST_HALF)
            {
                for (int i = 0;i < mPackageFirstHalf.Amount;i++)
                {
                    mPackagesRemain.Add(mPackageFirstHalf.Index(i));
                }
                return true;
            }
            else if (mGameStage == GameStage.SENCOND_HALF)
            {
                for (int i = 0;i < mPackageSecondHalf.Amount;i++)
                {
                    mPackagesRemain.Add(mPackageSecondHalf.Index(i));
                }
                return true;
            }
            else 
            {
                return false;
            }
        }

        private bool _GeneratePackage ()
        {
            if (mGameStage == GameStage.FIRST_HALF && 
                mGameTime >= mPackageFirstHalf.NextGenerationPackage().GenerationTime())
            {
                mPackagesRemain.Add(mPackageFirstHalf.GeneratePackage());
                return true;
            }
            else if (mGameStage == GameStage.SENCOND_HALF &&
                mGameTime >= mPackageSecondHalf.NextGenerationTime)
            {
                mPackagesRemain.Add(mPackageSecondHalf.GeneratePackage());
                return true;
            }
            else
            {
                return false;
            }
        }


        /***********************************************
        Time
        ***********************************************/
        private void _UpdateGameTime ()
        {
            mGameTime = _GetCurrentTime() - mStartTime;
        }

        private static int _GetCurrentTime()
        {
            System.DateTime currentTime = System.DateTime.Now;
            // time is in millisecond
            int time = currentTime.Hour * 3600000 + currentTime.Minute * 60000 + currentTime.Second * 1000;
            return time;
        }


        /***********************************************
        Judge Collision of illegal locations
        ***********************************************/
        private bool _IsOnBlackLine(Dot _CarPos)
        {
            return mBoundary.isCollided(_CarPos, COLLISION_RADIUS);
        }

        private bool _IsInObstacle (Dot _CarPos)
        {
            return mObstacle.isCollided(_CarPos, COLLISION_RADIUS);
        }

        private bool _IsInOpponentStation (Dot _CarPos)
        {
            if (mCamp == Camp.A)
            {
                return mChargeStation.isCollided(_CarPos, 2, COLLISION_RADIUS);
            } 
            else if (mCamp == Camp.B)
            {
                return mChargeStation.isCollided(_CarPos, 1, COLLISION_RADIUS);
            }
            else
            {
                throw new Exception("No team is racing now");
            }
        }

        private bool _IsInChargeStation(Dot _CarPos)
        {
            if (mCamp == Camp.NONE)
            {
                throw new Exception("No team is racing now");
            }
            else
            {
                return Station.isCollided(_CarPos, (int)mCamp, COLLISION_RADIUS);
            }
        }
    }

}