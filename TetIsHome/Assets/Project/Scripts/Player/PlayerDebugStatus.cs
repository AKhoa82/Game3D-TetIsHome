using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDebugStatus : MonoBehaviour
{
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        GameSession session = GameSession.Instance;

        // H: Mat 20 mau
        if (keyboard.hKey.wasPressedThisFrame)
            session.TakeDamage(20);

        // J: Mat 25 nang luong
        if (keyboard.jKey.wasPressedThisFrame)
            session.UseStamina(25);

        // K: Nhan 50 tien
        if (keyboard.kKey.wasPressedThisFrame)
            session.AddCoins(50);

        // L: Hien thong tin trong Console
        if (keyboard.lKey.wasPressedThisFrame)
        {
            Debug.Log("HP: " + session.Health
                + " | Stamina: " + session.Stamina
                + " | Coins: " + session.Coins);
        }
    }

    private void OnGUI()
    {
        GameSession s = GameSession.Instance;

        GUI.Box(
            new Rect(15, 15, 260, 125),
            "GAME SESSION - TEST");

        GUI.Label(new Rect(30, 45, 240, 25),
            "HP: " + s.Health + "/" + s.MaxHealth);

        GUI.Label(new Rect(30, 70, 240, 25),
            "Stamina: " + s.Stamina + "/" + s.MaxStamina);

        GUI.Label(new Rect(30, 95, 240, 25),
            "Coins: " + s.Coins);
    }
}