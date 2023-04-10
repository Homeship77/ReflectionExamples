using System;
using System.Collections.Generic;
using GameCtrlManagers;
using GameObjects;

namespace UnityEngine.EventSystems
{
    public interface IGlobalSubscriber
    {
    }

    public interface IGameEvent : IEventSystemHandler
    {
        void BallLostEvent(Vector2 lostPos, int sideID);
        void BallBouncing(Vector2 pos, Vector2 newVelocity);
    }

    public interface IServerNetworkEvent : IGlobalSubscriber
    {
        void ConnectPlayer(int pl_idx = -1);
        void DisconnectPlayer(int pl_idx);
        void SetNewBallColor(Color clr);
        void OpenColorPicker();
        void PlatformMove(int pl_idx, Vector3 deltaPos);
    }

    public interface IClientNetworkEvent : IGlobalSubscriber
    {
        IPlatform InitPlayer(Vector2 maxDim, SPlatformsData data);
        void UpdateClient(IGameObject[] gameObjects); //updating player's game state
        void DropPlayer();
        void InitPlayersUI(Color[] colors, int _bScore, float _updateTime);
        void UpdateClientUI(int[] score, float _time);
        void BallBounced(Vector2 pos, Vector2 velocity, float ballR);
    }

}

