﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDCHOST24
{
    //队名
    public enum Camp
    {
        NONE = 0, A, B
    };
    public class Car //选手的车
    {
        public const int RUN_CREDIT = 10;          //小车启动可以得到10分;
        public const int PICK_CREDIT = 5;          //接到一笔订单得5分;
        public const int ARRIVE_EASY_CREDIT = 20;  //规定时间内送达外卖可以得到20/25/30分;
        public const int ARRIVE_NORMAL_CREDIT = 25;
        public const int ARRIVE_HARD_CREDIT = 30;
        public const int CHARGE_CREDIT = 5;        //放置一个充电站可以得到5分;
        public const int LATE_PENALTY = 5;         //外卖超时1秒惩罚5分;
        public const int IGNORE_PENALTY = 5;       //未接单惩罚5分;
        public const int NON_GATE_PENALTY = 10;    //未经过规定出入口出入惩罚10分;
        public const int FOUL_PENALTY = 50;        //一个惩罚标记扣分50分;
        public const int MAX_PKG_COUNT = 5;        //车上最多携带的包裹数量
        public const int MAX_CHARGER_COUNT = 3;    //小车最多放置的充电桩数量

        public Dot mPos;
        public Dot mLastPos;
        public Dot mLastOneSecondPos;
        public Dot mTransPos;
        public Camp MyCamp;               //A or B get、set直接两个封装好的函数
        public int MyScore;               //得分
        
        public int mIsAbleToRun;          //小车是否能成功启动 0不能启动，1能启动
        public int mPickCount;            //小车接到的订单数量
        public int mArrivalCount;         //小车成功在规定时间内送达外卖个数
        public int mTaskState;            //小车任务 0为上半场任务，1为下半场任务
        public int mPkgCount;             //小车上放有外卖个数
        public int mChargeCount;          //小车放置的充电站个数
        public int mLateSeconds;          //小车送达外卖超时总秒数
        public int mNonGateCount;         //小车未经过规定出入口出入核心区的次数
        public int mIsInField;            //小车目前在不在核心区内 0不在核心区内 1在核心区内
        public int mIsInCharge;           //小车目前是否在充电区域内 0不在充电区内 1在充电区内
        public int mIsInObstacle;          
        public int mFoulCount;            //小车获得惩罚的个数
        
        /*Game里如果用不到就删
        public int mRightPos;             //小车现在的位置信息是否是正确的，0为不正确的，1为正确的
        public int mRightPosCount;        //用于记录小车位置是否该正确了
        */
        public List<Package> mPickedPackages;

        public Car(Camp c, int task)
        {
            MyCamp = c;
            mPos = new Dot(0, 0);
            mLastPos = new Dot(0, 0);
            mLastOneSecondPos = new Dot(0, 0);
            mTransPos = new Dot(0, 0);
            MyScore = 0;
            mIsAbleToRun = 0;
            mPickCount = 0;
            mArrivalCount = 0;
            mTaskState = task;
            mPkgCount = 0;
            mChargeCount = 0;
            mLateSeconds = 0;
            mNonGateCount = 0;
            mIsInField = 0;
            mIsInCharge = 0;
            mFoulCount = 0;
            mPickedPackages = new List<Package>();
            //mRightPos = 1;
            //mRightPosCount = 0;
        }
        public void UpdateLastPos()
        {
            mLastPos = mPos;
        }

        public void SetPos(Dot pos)
        {
            mPos = pos;
        }

        public void AddNonGatePunish() //扣分
        {
            mNonGateCount++; 
            mIsInField = !mIsInField;
            UpdateScore();
        }

          public void AddFoulCount()
        {
            mFoulCount++;
            UpdateScore();
        }

        public void AddChargeCount()   //放置充电站得分
        {
            if (mChargeCount < MAX_CHARGER_COUNT)
            {
            mChargeCount++;
            UpdateScore();
            }
        }
        public void CarRun()           //小车启动
        {
            mIsAbleToRun = 1;
            UpdateScore();
        }
        public void PickPackage(Package NewPackage)      //拾取外卖
        {
            if (mPkgCount < MAX_PKG_COUNT)
            {
                NewPackage.PickPackage();
                mPickedPackages.Add(NewPackage);
                mPkgCount++;
                mPickCount++;
                UpdateScore();
            }
        }

        public void DropPackage(Package PickedPackage)      //送达外卖 
        {
            if (mPkgCount > 0)
            {
                PickedPackage.DropPackage();
                UpdateScore();
                mPkgCount--;
            }
        }

        // ChargeTime is in millisecond
        public void Charge (int _ChargeTime)
        {

        }

        // ExtraDistance is in cm
        public void RunOutOfPower (int _ExtraDistance)
        {

        }

        private void UpdateScore()
        {
            MyScore = mIsAbleToRun * RUN_CREDIT
                + mPickCount * PICK_CREDIT
                + mChargeCount * CHARGE_CREDIT
                - mNonGateCOunt * NON_GATE_PENALTY
                - mFoulCount * FOUL_PENALTY;
            foreach(Package i in mPickedPackages)
            {
                MyScore += i.GetPackageScore();
            }
        }
    }
}