using UnityEngine;
using System;
using LastDay.Utilities;

namespace LastDay.Core
{
    public enum GameState
    {
        Loading,
        Playing,
        InDialogue,
        PhoneCall,
        Decision,
        Ending
    }

    public class GameStateMachine : Singleton<GameStateMachine>
    {
        public GameState CurrentState { get; private set; } = GameState.Loading;

        public event Action<GameState, GameState> OnStateChanged;

        public bool ChangeState(GameState newState)
        {
            if (CurrentState == newState) return false;

            if (!IsValidTransition(CurrentState, newState))
            {
                Debug.LogWarning($"[GameState] Invalid transition: {CurrentState} -> {newState}");
                return false;
            }

            GameState oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameState] {oldState} -> {newState}");
            OnStateChanged?.Invoke(oldState, newState);

            return true;
        }

        /// <summary>
        /// Force a state without validation. Use only for debug/recovery.
        /// </summary>
        public void ForceState(GameState state)
        {
            GameState oldState = CurrentState;
            CurrentState = state;
            Debug.LogWarning($"[GameState] FORCED: {oldState} -> {state}");
            OnStateChanged?.Invoke(oldState, state);
        }

        private bool IsValidTransition(GameState from, GameState to)
        {
            return (from, to) switch
            {
                (GameState.Loading, GameState.Playing) => true,
                (GameState.Playing, GameState.InDialogue) => true,
                (GameState.Playing, GameState.PhoneCall) => true,
                (GameState.Playing, GameState.Decision) => true,
                (GameState.InDialogue, GameState.Playing) => true,
                (GameState.PhoneCall, GameState.Playing) => true,
                (GameState.PhoneCall, GameState.InDialogue) => true,
                (GameState.Decision, GameState.Playing) => true,
                (GameState.Decision, GameState.Ending) => true,
                _ => false
            };
        }

        public bool CanPlayerMove => CurrentState == GameState.Playing;
        public bool CanInteract => CurrentState == GameState.Playing;
    }
}
