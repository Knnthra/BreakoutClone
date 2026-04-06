using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles the main menu navigation and actions.
/// Supports keyboard input (W/S or Up/Down) to select items and Space to confirm.
/// </summary>
public class MenuManager : MonoBehaviour
{
    /// <summary>
    /// Text element for the Start option.
    /// </summary>
    [SerializeField] private TextMeshProUGUI startText;

    /// <summary>
    /// Text element for the Mute/Unmute option.
    /// </summary>
    [SerializeField] private TextMeshProUGUI muteText;

    /// <summary>
    /// Text element for the Quit option.
    /// </summary>
    [SerializeField] private TextMeshProUGUI quitText;

    /// <summary>
    /// How fast the selected menu item blinks on and off in seconds.
    /// </summary>
    [SerializeField] private float blinkInterval = 0.4f;

    /// <summary>
    /// Animator for the joystick visual that plays Up/Down triggers on navigation.
    /// </summary>
    [SerializeField] private Animator joyStickAnimator;

    /// <summary>
    /// Sound effect played when navigating between menu items.
    /// </summary>
    [SerializeField] private AudioClip selectSound;

    /// <summary>
    /// Array of all menu item texts for indexed access.
    /// </summary>
    private TextMeshProUGUI[] menuItems;

    /// <summary>
    /// Index of the currently selected menu item.
    /// </summary>
    private int selectedIndex;

    /// <summary>
    /// Tracks whether audio is currently muted.
    /// </summary>
    private bool isMuted;

    /// <summary>
    /// Reference to the active blink coroutine so it can be stopped when selection changes.
    /// </summary>
    private Coroutine blinkCoroutine;

    /// <summary>
    /// Initializes the menu items, starts the blink effect, plays menu music,
    /// and disables the pixel art effect for the menu scene.
    /// </summary>
    private void Start()
    {
        menuItems = new[] { startText, muteText, quitText };
        selectedIndex = 0;
        UpdateSelection();
        AudioManager.Instance.PlayMenuMusic();
    }

    /// <summary>
    /// Checks for navigation input (W/S or Up/Down arrows) and confirm input (Space) each frame.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = menuItems.Length - 1;
            UpdateSelection();
            if (joyStickAnimator != null) joyStickAnimator.SetTrigger("Up");
            if (selectSound != null) AudioManager.Instance.PlaySFX(selectSound);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex++;
            if (selectedIndex >= menuItems.Length) selectedIndex = 0;
            UpdateSelection();
            if (joyStickAnimator != null) joyStickAnimator.SetTrigger("Down");
            if (selectSound != null) AudioManager.Instance.PlaySFX(selectSound);
        }

        if (Input.GetKeyDown(KeyCode.Space))
            Confirm();
    }

    /// <summary>
    /// Resets all menu items to full opacity and starts a blink coroutine on the selected item.
    /// </summary>
    private void UpdateSelection()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        for (int i = 0; i < menuItems.Length; i++)
        {
            menuItems[i].alpha = 1f;
        }

        blinkCoroutine = StartCoroutine(BlinkSelected());
    }

    /// <summary>
    /// Toggles the selected menu item's visibility on and off to create a blinking effect.
    /// </summary>
    private IEnumerator BlinkSelected()
    {
        while (true)
        {
            menuItems[selectedIndex].alpha = 0f;
            yield return new WaitForSeconds(blinkInterval);
            menuItems[selectedIndex].alpha = 1f;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    /// <summary>
    /// Executes the action for the currently selected menu item.
    /// Start loads the game scene, Mute toggles audio, Quit exits the application.
    /// </summary>
    private void Confirm()
    {
        switch (selectedIndex)
        {
            case 0:
                SceneManager.LoadScene(1);
                break;
            case 1:
                isMuted = AudioManager.Instance.ToggleMute();
                muteText.text = isMuted ? "Unmute" : "Mute";
                break;
            case 2:
                Application.Quit();
                break;
        }
    }
}
