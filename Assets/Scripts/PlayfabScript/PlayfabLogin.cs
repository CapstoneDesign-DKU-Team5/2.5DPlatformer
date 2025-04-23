using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

public class PlayFabLogin : MonoBehaviour
{
    [Header("Panels")]
    public GameObject registerPanel;
    public GameObject playfabUIPanel;

    [Header("Register Fields")]
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerUsernameInput;
    public Button playfabRegisterButton;
    public Button registerCloseButton;

    [Header("Login Fields")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;
    public Button loginRegisterButton; // 🔘 회원가입 창 열기 버튼

    [Header("UI Feedback")]
    public TMP_Text loginInfoText;

    private void Start()
    {
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
        {
            PlayFabSettings.staticSettings.TitleId = "12E6A8"; // 본인의 Title ID
        }

        // 초기에는 회원가입 패널 숨김
        registerPanel.SetActive(false);

        // 버튼 이벤트 연결
        playfabRegisterButton.onClick.AddListener(OnClickRegister);
        loginButton.onClick.AddListener(OnClickLogin);
        registerCloseButton.onClick.AddListener(HideRegisterPanel);
        loginRegisterButton.onClick.AddListener(ShowRegisterPanel); // 🔘 회원가입 버튼

        loginInfoText.text = "로그인 후 입장 가능합니다!";
    }

    private void OnClickLogin()
    {
        loginInfoText.text = "🔐 로그인 시도 중...";
        var request = new LoginWithEmailAddressRequest
        {
            Email = loginEmailInput.text,
            Password = loginPasswordInput.text,
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }
    private bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private void OnClickRegister()
    {
        string email = registerEmailInput.text.Trim();
        string password = registerPasswordInput.text;
        string username = registerUsernameInput.text.Trim();

        if (!IsValidEmail(email))
        {
            loginInfoText.text = "📧 올바른 이메일 형식이 아닙니다.";
            return;
        }

        if (password.Length < 6)
        {
            loginInfoText.text = "🔑 비밀번호는 최소 6자 이상이어야 합니다.";
            return;
        }

        if (string.IsNullOrEmpty(username))
        {
            loginInfoText.text = "👤 사용자 이름을 입력해주세요.";
            return;
        }

        loginInfoText.text = "📝 회원가입 시도 중...";
        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            Username = username,
            DisplayName = username,
            RequireBothUsernameAndEmail = true
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("로그인 성공! PlayFab ID: " + result.PlayFabId);
        loginInfoText.text = $"✅ 로그인 성공!\nPlayFab ID: {result.PlayFabId}";

        if (playfabUIPanel != null)
            playfabUIPanel.SetActive(false);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        Debug.Log("회원가입 성공! 새로운 계정이 생성되었습니다.");
        loginInfoText.text = "🎉 회원가입 성공! 자동 로그인 진행 중...";

        // 👉 회원가입에 사용된 정보를 그대로 로그인 시도
        var request = new LoginWithEmailAddressRequest
        {
            Email = registerEmailInput.text,
            Password = registerPasswordInput.text
        };
        loginEmailInput.text = registerEmailInput.text;
        loginPasswordInput.text = registerPasswordInput.text;
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }


    private void OnLoginFailure(PlayFabError error)
    {
        string userMessage = "❌ 오류 발생: ";

        switch (error.Error)
        {
            case PlayFabErrorCode.InvalidEmailAddress:
                userMessage += "올바르지 않은 이메일 형식입니다.";
                break;

            case PlayFabErrorCode.EmailAddressNotAvailable:
                userMessage += "이미 가입된 이메일입니다.";
                break;

            case PlayFabErrorCode.InvalidPassword:
            case PlayFabErrorCode.InvalidParams when error.ErrorDetails != null &&
                error.ErrorDetails.ContainsKey("Password"):
                userMessage += "비밀번호 형식이 올바르지 않습니다.\n비밀번호는 최소 6자 이상이며, 영어와 숫자를 포함해야 합니다.";
                break;

            case PlayFabErrorCode.UsernameNotAvailable:
                userMessage += "이미 사용 중인 사용자 이름입니다.";
                break;

            case PlayFabErrorCode.AccountNotFound:
            case PlayFabErrorCode.InvalidUsernameOrPassword:
                userMessage += "로그인에 실패했습니다. 이메일 또는 비밀번호를 다시 확인해주세요.";
                break;

            default:
                userMessage += "알 수 없는 오류가 발생했습니다.\n" +
                               "코드: " + error.Error.ToString();
                break;
        }

        loginInfoText.text = userMessage;
        Debug.LogError($"[PlayFab 오류] Code: {error.Error}, Message: {error.ErrorMessage}");
    }

    public void ShowRegisterPanel()
    {
        registerPanel.SetActive(true);
    }

    public void HideRegisterPanel()
    {
        registerPanel.SetActive(false);
    }
}
