using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class Chapter1InputReaderEditModeTests
    {
        [Test]
        public void PoliceArrestModeKeepsOnlyLookAndPauseAndRestoresCombatMode()
        {
            GameObject player = new GameObject("PoliceArrestInputTest");
            player.SetActive(false);

            Dictionary<string, InputAction> actions =
                new Dictionary<string, InputAction>();
            List<InputActionReference> references =
                new List<InputActionReference>();

            try
            {
                Chapter1InputReader reader =
                    player.AddComponent<Chapter1InputReader>();

                AddAction(
                    reader,
                    actions,
                    references,
                    "moveActionReference",
                    "Move",
                    InputActionType.Value);
                AddAction(
                    reader,
                    actions,
                    references,
                    "lookActionReference",
                    "Look",
                    InputActionType.Value);
                AddButton(reader, actions, references, "attackActionReference", "Attack");
                AddButton(reader, actions, references, "kickActionReference", "Kick");
                AddButton(reader, actions, references, "jumpActionReference", "Jump");
                AddButton(reader, actions, references, "sprintActionReference", "Sprint");
                AddButton(reader, actions, references, "crouchActionReference", "Crouch");
                AddButton(reader, actions, references, "interactActionReference", "Interact");
                AddButton(reader, actions, references, "talkActionReference", "Talk");
                AddButton(
                    reader,
                    actions,
                    references,
                    "toggleFlashlightActionReference",
                    "ToggleFlashlight");
                AddButton(reader, actions, references, "throwCanActionReference", "ThrowCan");
                AddButton(reader, actions, references, "inventoryActionReference", "Inventory");
                AddButton(reader, actions, references, "pauseActionReference", "Pause");

                player.SetActive(true);
                reader.SetCombatOnlyMode(true);

                Assert.IsTrue(actions["Move"].enabled);
                Assert.IsTrue(actions["Look"].enabled);
                Assert.IsTrue(actions["Attack"].enabled);
                Assert.IsTrue(actions["Kick"].enabled);
                Assert.IsTrue(actions["Jump"].enabled);
                Assert.IsTrue(actions["Sprint"].enabled);
                Assert.IsTrue(actions["Crouch"].enabled);
                Assert.IsTrue(actions["Interact"].enabled);
                Assert.IsTrue(actions["Pause"].enabled);
                Assert.IsFalse(actions["Talk"].enabled);
                Assert.IsFalse(actions["ThrowCan"].enabled);
                Assert.IsFalse(actions["Inventory"].enabled);
                Assert.IsFalse(actions["ToggleFlashlight"].enabled);

                SetProperty(reader, "MoveInput", Vector2.one);
                SetProperty(reader, "LookInput", Vector2.one);
                SetProperty(reader, "SprintHeld", true);
                SetProperty(reader, "TalkHeld", true);
                SetProperty(reader, "ThrowCanHeld", true);

                reader.SetPoliceArrestMode(true);

                Assert.IsTrue(reader.PoliceArrestMode);
                foreach (KeyValuePair<string, InputAction> entry in actions)
                {
                    bool shouldBeEnabled =
                        entry.Key == "Look" || entry.Key == "Pause";
                    Assert.AreEqual(
                        shouldBeEnabled,
                        entry.Value.enabled,
                        $"Unexpected police-arrest state for {entry.Key}.");
                }

                Assert.AreEqual(Vector2.zero, reader.MoveInput);
                Assert.AreEqual(Vector2.zero, reader.LookInput);
                Assert.IsFalse(reader.SprintHeld);
                Assert.IsFalse(reader.TalkHeld);
                Assert.IsFalse(reader.ThrowCanHeld);

                int attackCount = 0;
                int interactCount = 0;
                int pauseCount = 0;
                reader.AttackPressed += () => attackCount++;
                reader.InteractPressed += () => interactCount++;
                reader.PausePressed += () => pauseCount++;

                InvokeCallback(reader, "OnAttackPerformed");
                InvokeCallback(reader, "OnInteractPerformed");
                InvokeCallback(reader, "OnPausePerformed");

                Assert.AreEqual(0, attackCount);
                Assert.AreEqual(0, interactCount);
                Assert.AreEqual(1, pauseCount);

                reader.SetPoliceArrestMode(false);

                Assert.IsFalse(reader.PoliceArrestMode);
                Assert.IsTrue(reader.CombatOnlyMode);
                Assert.IsTrue(actions["Move"].enabled);
                Assert.IsTrue(actions["Look"].enabled);
                Assert.IsTrue(actions["Interact"].enabled);
                Assert.IsTrue(actions["Pause"].enabled);
                Assert.IsFalse(actions["Talk"].enabled);
                Assert.IsFalse(actions["Inventory"].enabled);
            }
            finally
            {
                player.SetActive(false);
                UnityEngine.Object.DestroyImmediate(player);

                for (int i = 0; i < references.Count; i++)
                {
                    UnityEngine.Object.DestroyImmediate(references[i]);
                }

                foreach (InputAction action in actions.Values)
                {
                    action.Dispose();
                }
            }
        }

        private static void AddButton(
            Chapter1InputReader reader,
            IDictionary<string, InputAction> actions,
            ICollection<InputActionReference> references,
            string fieldName,
            string actionName)
        {
            AddAction(
                reader,
                actions,
                references,
                fieldName,
                actionName,
                InputActionType.Button);
        }

        private static void AddAction(
            Chapter1InputReader reader,
            IDictionary<string, InputAction> actions,
            ICollection<InputActionReference> references,
            string fieldName,
            string actionName,
            InputActionType actionType)
        {
            InputAction action = new InputAction(actionName, actionType);
            InputActionReference actionReference =
                InputActionReference.Create(action);
            actions.Add(actionName, action);
            references.Add(actionReference);
            SetField(reader, fieldName, actionReference);
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private static void SetProperty(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property, propertyName);
            property.SetValue(target, value);
        }

        private static void InvokeCallback(
            Chapter1InputReader reader,
            string methodName)
        {
            MethodInfo method = typeof(Chapter1InputReader).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, methodName);
            method.Invoke(
                reader,
                new object[] { default(InputAction.CallbackContext) });
        }
    }
}
