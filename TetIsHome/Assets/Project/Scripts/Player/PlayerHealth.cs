using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 2f;

    private CharacterController controller;
    private PlayerMovement movement;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private bool isDead = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        // Luu vi tri xuat hien trong Scene hien tai
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    private void Update()
    {
        if (isDead)
            return;

        if (GameSession.Instance.Health <= 0)
            StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        isDead = true;

        Debug.Log("Nhan vat da het mau!");

        // Tam dung di chuyen
        if (movement != null)
            movement.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // Dua Player ve diem xuat hien
        controller.enabled = false;

        transform.SetPositionAndRotation(
            spawnPosition,
            spawnRotation
        );

        controller.enabled = true;

        // Khoi phuc mau va nang luong
        GameSession session = GameSession.Instance;

        session.Heal(session.MaxHealth);
        session.RestoreStamina(session.MaxStamina);

        // Cho phep di chuyen lai
        if (movement != null)
        {
            movement.ResetVerticalVelocity();
            movement.enabled = true;
        }

        isDead = false;

        Debug.Log("Nhan vat da hoi sinh!");
    }
}
