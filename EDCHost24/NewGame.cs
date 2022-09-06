using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Security.Cryptography;
using System.Diagnostics;

namespace EDCHOST24
{
    // Token
    public enum GameState { UNSTART = 0, RUN = 1};

    public enum GameStage { PREPARATION = 0, FIRST_HALF = 1,  SENCOND_HALF= 2, END = 3};

    public class Game
    {
        // size of competition area
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
        private Station mChargeStationA;
        private Station mChargeStationB;

        // which team is racing A or B
        private Camp mCamp;

        // obstacle
        private Labyrinth mObstacle;

        private Boundary mBoundary;

        
        /***********************************************
        Constructor
        No parameter needs
        ***********************************************/
        public Game()
        {
            Debug.WriteLine("Call Constructor of Game");

            mGameState = GameState.UNSTART;
            mGameStage = GameStage.PREPARATION;

            mCamp = Camp.NONE;

            mCarA = new Car(Camp.A, 0);
            mCarB = new Car(Camp.B, 0);

            mScoreA = new int() {0,0};
            mScoreB = new int() {0.0};

            // Generate the package series for first and second half
            mPackageFirstHalf = new PackageList(AVAILIABLE_MAX_X, AVAILIABLE_MIN_X, 
                        AVAILIABLE_MAX_Y, AVAILIABLE_MIN_Y, INITIAL_PKG_NUM, FIRST_HALF_TIME, TIME_INTERVAL);
            mPackageSecondHalf = new PackageList(AVAILIABLE_MAX_X, AVAILIABLE_MIN_X, 
                        AVAILIABLE_MAX_Y, AVAILIABLE_MIN_Y, INITIAL_PKG_NUM, SECOND_HALF_TIME, TIME_INTERVAL);

            mChargeStationA = new Station ();
            mChargeStationB = new Station ();

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
            _UpdateGameTime();

            // Try to generate packages on each refresh
            _GeneratePackage();

            int TimePenalty = 0;
            // Team A is on racing
            if (mCamp == Camp.A)
            {
                TimePenalty = mCarA.Update(_CarPos, mGameTime, _IsOnBlackLine(_CarPos), 
                _IsInObstacle(_CarPos), _IsInOpponentStation(_CarPos), 
                _IsInChargeStation(_CarPos), mPackagesRemain);
            }
            else if (mCamp == Camp.B)
            {
                TimePenalty = mCarA.Update(_CarPos, mGameTime, _IsOnBlackLine(_CarPos), 
                _IsInObstacle(_CarPos), _IsInOpponentStation(_CarPos), 
                _IsInChargeStation(_CarPos), mPackagesRemain);
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

            if (_GameStage != GameStage.FIRST_HALF || _GameStage != GameStage.SENCOND_HALF)
            {
                Debug.WriteLine("Failed to change game stage! Expect input to be GameStage.FIRST_HALF or GameStage.SECOND_HALF");
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
                
            }

            mStartTime = _GetCurrentTime();
            mGameTime = 0;
        }

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

            // set state param of game
            mGameState = GameState.UNSTART;
            mGameStage = GameStage.PREPARATION;
            mCamp = Camp.NONE;

            mPackagesRemain.Clear();

            mStartTime = -1;
            mGameTime = -1;
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
                mGameTime >= mPackageFirstHalf.NextGenerationTime)
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

        private static int GetCurrentTime()
        {
            System.DateTime currentTime = System.DateTime.Now;
            int time = currentTime.Hour * 3600000 + currentTime.Minute * 60000 + currentTime.Second * 1000;
            //Debug.WriteLine("H, M, S: {0}, {1}, {2}", currentTime.Hour, currentTime.Minute, currentTime.Second);
            //Debug.WriteLine("GetCurrentTime，Time = {0}", time); 
            return time;
        }


        /***********************************************
        Penalty for Access illegal Area
        ***********************************************/
        private void _Penalty (ref Car _car)
        {
            if ((_IsOutOfCompetitionArea(_car) && _car.mIsInField) ||
                (!_IsOutOfCompetitionArea(_car) && !_car.mIsInField) )
            {
                _car.AddNonGatePunish();
            }

            if ((_IsInObstacle(_car) && _car.mIsInObstacle) ||
                (!_IsInObstacle(_car) && !_car.mIsInObstacle))
            {
                _car.InObstacle();
            }

            if (mGameStage == GameStage.SENCOND_HALF && 
                ((_IsInOpponentStation(_car) && _car.mIsInOpponentChargeStation) ||
                (!_IsInObstacle(_car) && !_car.mIsInOpponentChargeStation)))
            {
                _car.InOpponentStation();
            }
        }

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
                return mChargeStationB.isCollided(_CarPos, COLLISION_RADIUS);
            } 
            else if (mCamp == Camp.B)
            {
                return mChargeStationA.isCollided(_CarPos, COLLISION_RADIUS);
            }
            else
            {
                throw new Exception("No team is racing now");
            }
        }

        private bool _IsInChargeStation(Dot _CarPos)
        {
            if (mCamp == Camp.A)
            {
                return mChargeStationA.isCollided(_CarPos, COLLISION_RADIUS);
            } 
            else if (mCamp == Camp.B)
            {
                return mChargeStationB.isCollided(_CarPos, COLLISION_RADIUS);
            }
            else
            {
                throw new Exception("No team is racing now");
            }
        }

        /***********************************************
        Set Charge Station in First-Half
        ***********************************************/
        private void _SetChargeStation (ref Car _car) 
        {
            if (mGameStage == GameStage.SENCOND_HALF) 
            {
                return;
            }

            if (_car.AddChargeCount())
            {
                if (_car.MyCamp == Camp.A)
                {
                    mChargeStationA.Add(mCarA.GetCarPos(0));
                }
                else if (_car.MyCamp == Camp.B)
                {
                    mChargeStationB.Add (mCarB.GetCarPos(0));
                }
            }
        }

        /***********************************************
        Time
        ***********************************************/
        private static int _GetCurrentTime()
        {
            System.DateTime currentTime = System.DateTime.Now;
            int time = currentTime.Hour * 3600000 + currentTime.Minute * 60000 + currentTime.Second * 1000;
            //Debug.WriteLine("H, M, S: {0}, {1}, {2}", currentTime.Hour, currentTime.Minute, currentTime.Second);
            //Debug.WriteLine("GetCurrentTime，Time = {0}", time); 
            return time;
        }

        private void _UpdateGameTime ()
        {
            mGameTime = _GetCurrentTime() - mStartTime;
        }
    }

}