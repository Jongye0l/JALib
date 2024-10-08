# JAMod 개발 가이드 - 기능 설정하기
### [목차로 이동](DevelopGuide.md)
1. [JAFeature 상속](#feature-%EC%83%81%EC%86%8D)
2. [JAFeature 이벤트](#feature-%EC%9D%B4%EB%B2%A4%ED%8A%B8)
3. [JAFeature 변수](#feature-%EB%B3%80%EC%88%98)
4. [예시 코드](#%EC%98%88%EC%8B%9C-%EC%BD%94%EB%93%9C)

## Feature 상속
우선 기능을 생성하기 위해서는 기능에 메인이 되는 클래스에 'Feature'를 상속받아야 합니다.
```csharp
public class MyFeature : Feature
```

Feature는 생성을 모드에서 직접 해줘야 하므로 생성자 설정은 아무렇게나 해도 상관 없습니다.

base 생성자에서 사용할 수 있는 인자는 다음과 같습니다. (필수)를 제외하고 추가하고는 싶을 경우에만 추가할 수 있습니다.
* **JAMod mod: 모드입니다.(필수)**
* **string name: 기능의 이름입니다.(필수)**
* bool canEnable: 기능을 활성화 상태를 토글 할 수 있는지 여부입니다.
* Type patchClass: 패치 클래스를 지정합니다.
* Type settingType: 기능 설정 클래스를 지정합니다.

## Feature 이벤트
Feature의 생성자를 설정하고 나면 다음과 같은 이벤트를 사용할 수 있습니다.

### OnEnable
기능이 활성화 되었을 때 실행되는 이벤트입니다.
```csharp
protected override void OnEnable() {
    base.OnEnable();
}
```

### OnDisable
기능이 비활성화 되었을 때 실행되는 이벤트입니다.
```csharp
protected override void OnDisable() {
    base.OnDisable();
}
```

### OnUnload
기능이 언로드 되었을 때 실행되는 이벤트입니다.
```csharp
protected override void OnUnload() {
    base.OnUnload();
}
```

### OnGUI
기능 설정창의 GUI를 그릴 때 실행되는 이벤트입니다.
```csharp
protected override void OnGUI() {
    base.OnGUI();
}
```

### OnShowGUI
기능 설정창이 열릴 때 실행되는 이벤트입니다.
```csharp
protected override void OnShowGUI() {
    base.OnShowGUI();
}
```

### OnHideGUI
기능 설정창이 닫힐 때 실행되는 이벤트입니다.
```csharp
protected override void OnHideGUI() {
    base.OnHideGUI();
}
```

## Feature 변수
Feature에서 사용할 수 있는 변수는 다음과 같습니다.

Protected는 Feature가 상속된 코드에서만 사용할 수 있습니다.

Feature에 변수는 '지정 가능'을 제외하고는 변수 설정이 불가능 합니다.

### Enabled
 * Public
 * 지정 가능
 * bool
 * 기능을 활성화 하였는지 여부입니다.
 * 지정시 기능을 끄거나 킬 수 있습니다.

### Active
 * Public
 * bool
 * 기능이 현재 활성화 되었는지 여부입니다.

### CanEnable
 * Public
 * 지정 가능(Protected)
 * bool
 * 기능의 활성화 상태를 토글 할 수 있는지 여부입니다.
 * 기능의 활성화 상태를 토글 할 수 없다면 항상 활성화 되게 됩니다.
 * 해당 기능이 꺼져있어도 Enabled 변수를 통해 활성화 상태를 변경할 수 있습니다.

### Setting
 * Public
 * JASetting
 * 기능의 설정입니다.

### Mod
 * Public
 * JAMod
 * 기능이 속한 모드입니다.

### Name
 * Public
 * string
 * 기능의 이름입니다.

### Patcher
 * Protected
 * JAPatcher
 * 기능의 패치를 관리하는 도구입니다.

## 예시 코드
다음은 기능을 생성하고 설정하는 예시 코드입니다.
```csharp
public class MyFeature : Feature {
    public MyFeature() : base(Main.Instance, nameof(MyFeature), patchClass: typeof(MyFeature)) {
    }

    protected override void OnEnable() {
        Main.Instance.Log("내 기능 집에 가고 싶음");
    }

    protected override void OnDisable() {
        Main.Instance.Log("내 기능 집에감");
    }
    
    // 패치 매서드 사용 가능
}
```

## [다음](SetupSetting.md)
