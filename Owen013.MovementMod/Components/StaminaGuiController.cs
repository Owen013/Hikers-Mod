using UnityEngine;

namespace HikersMod.Components;

public class StaminaGuiController : MonoBehaviour
{
    private Font _owFont;

    private void Start()
    {
        _owFont = Resources.Load<Font>(@"fonts/english - latin/SpaceMono-Regular_Dynamic");
    }

    private void OnGUI()
    {
        if (GUIMode.IsHiddenMode() || PlayerState.UsingShipComputer()) return;

        GUI.Label(new Rect(0f, 0f, 300f, 60f), $"Stamina: {SprintingController.Instance.StaminaSecondsLeft / Config.StaminaSeconds}%", new GUIStyle { font = _owFont, fontSize = 30, wordWrap = false });
    }
}