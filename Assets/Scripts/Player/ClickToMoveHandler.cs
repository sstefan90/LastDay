using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using LastDay.Core;
using LastDay.Interaction;
using LastDay.NPC;

namespace LastDay.Player
{
    public class ClickToMoveHandler : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private LayerMask walkableLayer;
        [SerializeField] private LayerMask characterLayer;

        [Header("Click Feedback")]
        [SerializeField] private GameObject clickIndicatorPrefab;

        private GameObject currentIndicator;
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        void Update()
        {
            if (IsPointerOverUI())
                return;

            if (GameStateMachine.Instance != null && !GameStateMachine.Instance.CanInteract)
                return;

            if (Input.GetMouseButtonDown(0))
                HandleClick();
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            raycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                if (raycastResults[i].gameObject.layer == LayerMask.NameToLayer("UI"))
                    return true;
            }

            return false;
        }

        private void HandleClick()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            Vector2 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);

            if (PlayerController2D.Instance == null) return;

            // Check interactable objects first
            Collider2D interactableHit = Physics2D.OverlapPoint(mouseWorldPos, interactableLayer);
            if (interactableHit != null)
            {
                var interactable = interactableHit.GetComponent<InteractableObject2D>();
                if (interactable != null)
                {
                    PlayerController2D.Instance.MoveToAndInteract(interactable);
                    ShowClickIndicator(interactable.transform.position);
                    return;
                }
            }

            // Check NPC characters (Martha)
            Collider2D characterHit = Physics2D.OverlapPoint(mouseWorldPos, characterLayer);
            if (characterHit != null && characterHit.gameObject != gameObject)
            {
                var npc = characterHit.GetComponent<NPCController>();
                if (npc != null)
                {
                    PlayerController2D.Instance.MoveToAndTalk(npc);
                    ShowClickIndicator(npc.transform.position);
                    return;
                }
            }

            // Then check walkable area
            Collider2D walkableHit = Physics2D.OverlapPoint(mouseWorldPos, walkableLayer);
            if (walkableHit != null)
            {
                PlayerController2D.Instance.MoveTo(mouseWorldPos);
                ShowClickIndicator(mouseWorldPos);
            }
        }

        private void ShowClickIndicator(Vector2 position)
        {
            if (clickIndicatorPrefab == null) return;

            if (currentIndicator != null)
                Destroy(currentIndicator);

            currentIndicator = Instantiate(clickIndicatorPrefab, position, Quaternion.identity);
            Destroy(currentIndicator, 0.5f);
        }
    }
}
