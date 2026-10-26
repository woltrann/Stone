using System;
using System.Collections.Generic;
using TidyUpFramework.Audio;
using TidyUpFramework.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Screen Stack")]
    public sealed class TidyScreenStack : MonoBehaviour
    {
        [SerializeField, Tooltip("Screen shown when the stack is empty, for example the main menu panel.")]
        TidyUIScreen rootScreen;

        [SerializeField, Tooltip("Escape and the gamepad East button close the top screen.")]
        bool handleBackInput = true;

        readonly List<TidyUIScreen> stack = new List<TidyUIScreen>();
        int lastChangeFrame = -1;

        public event Action<TidyUIScreen> TopChanged;
        public event Action StackEmptied;

        public TidyUIScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : null;
        public int Count => stack.Count;
        public bool HandleBackInput
        {
            get => handleBackInput;
            set => handleBackInput = value;
        }

        public void Push(TidyUIScreen screen)
        {
            if (screen == null || Top == screen)
            {
                return;
            }

            if (Top != null)
            {
                Top.Hide();
            }
            else if (rootScreen != null && rootScreen != screen)
            {
                rootScreen.Hide();
            }

            stack.Remove(screen);
            stack.Add(screen);
            lastChangeFrame = Time.frameCount;
            screen.Show();
            TopChanged?.Invoke(screen);
        }

        public void Pop()
        {
            if (stack.Count == 0)
            {
                return;
            }

            var top = Top;
            stack.RemoveAt(stack.Count - 1);
            top.Hide();
            if (Top != null)
            {
                Top.Show();
                TopChanged?.Invoke(Top);
                return;
            }

            if (rootScreen != null)
            {
                rootScreen.Show();
            }

            StackEmptied?.Invoke();
            TopChanged?.Invoke(rootScreen);
        }

        public void Clear()
        {
            while (stack.Count > 0)
            {
                Pop();
            }
        }

        public void ShowRoot()
        {
            if (rootScreen != null)
            {
                rootScreen.Show();
            }
        }

        void Update()
        {
            if (!handleBackInput || stack.Count == 0 || Time.frameCount == lastChangeFrame)
            {
                return;
            }

            var keyboardBack = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            var gamepadBack = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            if (keyboardBack || gamepadBack)
            {
                if (TidyFramework.Settings != null)
                {
                    TidyAudio.Play(TidyFramework.Settings.UiBackCue);
                }

                Pop();
            }
        }
    }
}
