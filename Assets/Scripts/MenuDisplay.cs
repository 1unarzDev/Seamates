using UnityEngine;

public class MenuDisplay : MonoBehaviour
{
    [SerializeField] GameObject _menu;
    private bool _displayMenu;

    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Q)) {
            _displayMenu = !_displayMenu;
        }
        
        _menu.SetActive(_displayMenu);
    }
}