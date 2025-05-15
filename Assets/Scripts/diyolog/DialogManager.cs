using UnityEngine;
using TMPro;

public class DialogManager : MonoBehaviour
{
    public string[] dialogLines;
    public TextMeshProUGUI dialogText; // Diyalog yaz�s�
    public GameObject[] storedElements; // Kapan�p a��lacak objeler

    private int currentLine = 0;
    private bool isDialogActive = false;

    public GameObject player;
    public SC_FPSController fpsController;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        fpsController = player.GetComponent<SC_FPSController>();
    }

    void Update()
    {
        if (isDialogActive && Input.GetKeyDown(KeyCode.Space))
        {
            NextLine();
        }
    }

    public void StartDialog()
    { 
        currentLine = 0;
        isDialogActive = true;
        dialogText.gameObject.SetActive(true);
        dialogText.text = dialogLines[currentLine];

        // FPS kontrol�n� kapat
        fpsController.enabled = false;

        // Mouse imlecini a�
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Belirlenen objeleri kapat
        foreach (GameObject obj in storedElements)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    void NextLine()
    {
        currentLine++;
        if (currentLine < dialogLines.Length)
        {
            dialogText.text = dialogLines[currentLine];
        }
        else
        {
            EndDialog();
        }
    }

    void EndDialog()
    {
        isDialogActive = false;

        // FPS kontrol�n� geri a�
        fpsController.enabled = true;
        dialogText.gameObject.SetActive(false);
        // Mouse imlecini gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Objeleri geri a�
        foreach (GameObject obj in storedElements)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }
}
