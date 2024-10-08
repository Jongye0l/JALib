# JAMod 개발 가이드 - JA모드 설정하기
### [목차로 이동](DevelopGuide.md)
1. [JAMod 상속](#JAMod 상속)
2. [JAMod 이벤트](#JAMod 이벤트)
3. [JAMod 변수](#JAMod 변수)
4. [JAMod 함수](#JAMod 함수)
5. [예시 코드](#예시 코드)

## JAMod 상속
우선 JAMod를 만들기 위해서는 모드에 메인이 되는 클래스에 'JAMod'를 상속받아야 합니다.
```csharp
public class Main : JAMod
```
JAMod의 생성자는 다음과 같이 설정해야 됩니다. (생성자는 public, private 상관 없지만 private으로 해두는 것을 추천합니다.)
```csharp
private Main(UnityModManager.ModEntry modEntry) : base(modEntry, false)
```
Templete을 사용할 경우 기본적으로 존재하는 클래스 입니다.

base 생성자에서 사용할 수 있는 인자는 다음과 같습니다. (필수)를 제외하고 추가하고는 싶을 경우에만 추가할 수 있습니다.
* **UnityModManager.ModEntry modEntry: 유니티 모드의 코어입니다.(필수)**
* **bool localization: 언어 데이터를 로드할지 여부입니다.(필수)**
  * 현재 언어 기능은 종열만 사용 가능합니다.
* Type settingType: 모드 설정 클래스를 지정합니다.
* string settingPath: 모드 설정 파일의 경로를 지정합니다.
* string discord: 모드와 관련된 디스코드 링크를 지정합니다.
* int gid: localization의 gid를 지정합니다.
  * 현재 언어 기능은 종열만 사용 가능합니다.

## JAMod 이벤트
JAMod의 생성자를 설정하고 나면 다음과 같은 이벤트를 사용할 수 있습니다.

### OnEnable
모드가 활성화 되었을 때 실행되는 이벤트입니다.
```csharp
protected override void OnEnable() {
    base.OnEnable();
}
```

### OnDisable
모드가 비활성화 되었을 때 실행되는 이벤트입니다.
```csharp
protected override void OnDisable() {
    base.OnDisable();
}
```

### OnUnload
모드가 언로드 되었을 때 실행되는 이벤트입니다.
```csharp
protected override void OnUnload() {
    base.OnUnload();
}
```

### OnGUI
모드 설정창의 GUI를 그릴 때 실행되는 이벤트입니다.
```csharp
protected override void OnGUI() {
    base.OnGUI();
}
```

### OnGUIBehind
모드 설정창의 GUI를 그릴 때 실행되는 이벤트입니다. (기능(Feature) GUI를 그린 후 실행됩니다.)
```csharp
protected override void OnGUIBehind() {
    base.OnGUIBehind();
}
```

### OnShowGUI
모드 설정창을 열었을 때 실행되는 이벤트입니다.
```csharp
protected override void OnShowGUI() {
    base.OnShowGUI();
}
```

### OnHideGUI
모드 설정창을 닫았을 때 실행되는 이벤트입니다.
```csharp
protected override void OnHideGUI() {
    base.OnHideGUI();
}
```

### OnUpdate
프레임이 업데이트 될 때 실행되는 이벤트입니다.
```csharp
protected override void OnUpdate(float deltaTime) {
    base.OnUpdate();
}
```

### OnFixedUpdate
일정한 시간 간격으로 실행되는 이벤트입니다.
```csharp
protected override void OnFixedUpdate(float deltaTime) {
    base.OnFixedUpdate();
}
```

### OnLateUpdate
프레임이 업데이트 된 후 실행되는 이벤트입니다.
```csharp
protected override void OnLateUpdate(float deltaTime) {
    base.OnLateUpdate();
}
```

### OnSessionStart
UMM에서 모드 설정이 완료된 후 실행되는 이벤트입니다.

**해당 기능은 UMM 0.27.0 이상에서만 사용 가능합니다.**
```csharp
protected override void OnSessionStart() {
    base.OnSessionStart();
}
```

### OnSessionEnd
UMM에서 모드 종료 작업이 완료된 후 실행되는 이벤트입니다.

**해당 기능은 UMM 0.27.0 이상에서만 사용 가능합니다.**
```csharp
protected override void OnSessionEnd() {
    base.OnSessionEnd();
}
```

### OnLocalizationUpdate
언어 데이터가 업데이트 되었을 때 실행되는 이벤트입니다.
```csharp
protected override void OnLocalizationUpdate() {
    base.OnLocalizationUpdate();
}
```

## JAMod 변수
JAMod에서는 다음과 같은 변수를 지원합니다.

Protected는 JAMod가 상속된 코드에서만 사용할 수 있습니다.

JAMod에 변수는 '지정 가능'을 제외하고는 변수 설정이 불가능 합니다.

### ModEntry
 * Protected
 * UnityModManager.ModEntry
 * 모드의 ModEntry입니다.

### Logger
 * Public
 * UnityModManager.ModEntry.ModLogger
 * 모드의 Logger입니다.
 * Logger를 불러와서 사용할 수 있지만 JA모드 자체에 로그 함수가 있기 때문에 사용할 필요가 없습니다.

### Name
 * Public
 * string
 * 모드의 이름입니다.

### Version
 * Public
 * Version
 * 모드의 버전입니다.

### Path
 * Public
 * string
 * 모드 폴더의 경로입니다.

### LatestVersion
 * Protected
 * Version
 * 모드의 최신 버전입니다.

### IsLatest
 * Public
 * bool
 * 모드가 최신 버전인지 여부입니다.

### Features
 * Protected
 * List<Feature>
 * 모드의 기능 목록입니다.

### AvailableLanguages
 * Protected
 * SystemLanguage[]
 * 사용 가능한 언어 목록입니다.

### Setting
 * Protected
 * JASetting
 * 모드의 설정입니다.

### Discord
 * Protected
 * 지정 가능
 * string
 * 모드와 관련된 디스코드 링크입니다.

### Enabled
 * Public
 * bool
 * 모드가 활성화 되었는지 여부입니다.

### CustomLanguage
 * Protected
 * 지정 가능
 * SystemLanguage
 * 사용자 정의 언어 입니다.

### Localization
 * Public
 * Localization
 * 모드의 언어 데이터입니다.

## JAMod 함수
JAMod에서는 다음과 같은 함수를 지원합니다.

Protected는 JAMod가 상속된 코드에서만 사용할 수 있습니다.

### AddFeature(params Feature[])
 * Protected
 * void
 * 모드에 기능을 추가합니다.
```csharp
AddFeature(new Feature(), new Feature());
```

### Enable()
 * Public
 * void
 * 모드를 활성화 합니다.
```csharp
mod.Enable();
```

### Disable()
 * Public
 * void
 * 모드를 비활성화 합니다.
```csharp
mod.Disable();
```

### Log(Object)
 * Public
 * void
 * 모드의 로그를 출력합니다.
```csharp
mod.Log("Log");
```

### Warning(Object)
 * Public
 * void
 * 모드의 경고 로그를 출력합니다.
```csharp
mod.Warning("Warning");
```

### Error(Object)
 * Public
 * void
 * 모드의 오류 로그를 출력합니다.
```csharp
mod.Error("Error");
```

### Critical(Object)
 * Public
 * void
 * 모드의 치명적인 오류 로그를 출력합니다.
```csharp
mod.Critical("Critical");
```

### NativeLog(Object)
 * Public
 * void
 * 모드의 로그를 출력합니다.
 * 해당 매서드로 출력된 로그는 파일에서만 확인 가능합니다.
```csharp
mod.NativeLog("NativeLog");
```

### LogException(String, Exception)
 * Public
 * void
 * 모드의 예외 로그를 출력합니다.
```csharp
mod.LogException("Fail To Exception", new Exception());
```

### LogException(Exception)
 * Public
 * void
 * 모드의 예외 로그를 출력합니다.
```csharp
mod.LogException(new Exception());
```

### SaveSetting()
 * Public
 * void
 * 모드의 설정을 저장합니다.
```csharp
mod.SaveSetting();
```

## 예시 코드
다음은 JAMod를 상속받은 클래스의 예시 코드입니다.
```csharp
namespace JAMod;

public class Main : JAMod {
    private Main(UnityModManager.ModEntry modEntry) : base(modEntry, false, discord: "https://discord.jongyeol.kr") {
        // 모드 생성시 실행시킬 코드
        AddFeature(new Feature());
    }
  
    protected override void OnEnable() {
        // 모드 활성화시 실행시킬 코드
        Log("집에 가고 싶다");
    }
  
    protected override void OnDisable() {
        // 모드 비활성화시 실행시킬 코드
        Log("집에 감");
    }
  
    protected override void OnGUI() {
         // 모드 설정창 GUI를 그릴 때 실행시킬 코드
         GUILayout.Label("집에 가고 싶다");
    }
}
```

## [다음](SetupFeature.md)
