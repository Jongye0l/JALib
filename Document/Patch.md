# JAMod 개발 가이드 - 패치
### [목차로 이동](DevelopGuide.md)
1. [패치 매서드 생성](#%ED%8C%A8%EC%B9%98-%EB%A7%A4%EC%84%9C%EB%93%9C-%EC%83%9D%EC%84%B1)
2. [Prefix](#Prefix)
3. [Postfix](#Postfix)
4. [Transpiler](#Transpiler)
5. [Finalizer](#Finalizer)
6. [Replace](#Replace)

## 패치 매서드 생성
패치를 하기 위해서는 다음과 같이 패치 매서드를 생성해야 합니다.
```csharp
[JAPatch(typeof(scnGame), "Play", PatchType.Postfix, false)]
private static void OnGameStart()
```
패치 매서드는 static으로 선언해야 됩니다.

패치의 종류는 다음과 같습니다.
 * Prefix: 메서드 실행 전에 실행됩니다.
 * Postfix: 메서드 실행 후에 실행됩니다.
 * Transpiler: 메서드 실행 코드를 변경합니다.
 * Finalizer: 메서드 실행중 Exception이 발생하면 실행됩니다.
 * Replace: 메서드 전체 코드를 해당 코드로 변경합니다.

기본적으로 [Harmony 라이브러리](https://harmony.pardeike.net/index.html)에 패치를 의존하기 때문에 더 자세한 내용이 궁금하다면 [Harmony 사이트](https://harmony.pardeike.net/index.html)에서 확인해주세요.

패치 Attribute는 한개가 아닌 여러개를 사용하여 다중으로 사용하는것이 가능합니다.
```csharp
[JAPatch(typeof(scnGame), "Play", PatchType.Postfix, false)]
[JAPatch(typeof(scrPressToStart), "ShowText", PatchType.Postfix, false)]
private static void OnGameStart()
```
패치에 생성자는 다음과 같습니다.
 * Type classType, string methodName, PatchType patchType, bool disable
 * string className, string methodName, PatchType patchType, bool disable
 * MethodBase method, PatchType patchType, bool disable
 * Delegate @delegate, PatchType patchType, bool disable

밑에 2개 생성자 같은 경우 Attribute로는 사용할 수 없습니다.

패치 생성자에 추가할 수 있는 요소는 다음과 같습니다.
 * int MinVersion: 패치가 적용되는 최소 버전(r)입니다.
 * int MaxVersion: 패치가 적용되는 최대 버전(r)입니다.
 * string[] ArgumentTypes: 패치 매서드의 인자 타입입니다.
 * Type[] ArgumentTypesType: 패치 매서드의 인자 타입입니다.
 * MethodInfo Method: 패치 매서드입니다.
 * string GenericName: 패치 매서드의 제네릭 이름입니다.
 * Type GenericType: 패치 매서드의 제네릭 타입입니다.
 * bool TryingCatch: 패치 매서드에서 예외를 잡을지 여부입니다.(기본값: true)

패치를 추가하는 방법은 여러가지가 있습니다.

첫 번째 방법은 클래스 안에 있는 모든 매서드중 패치 Attribute가 있는 매서드를 패치하는 방법입니다.
```csharp
Patcher.AddPatch(typeof(PatchType));
```
두 번째 방법은 MethodInfo를 이용하여 패치하는 방법입니다.
```csharp
Patcher.AddPatch(typeof(PatchType).Method("PatchMethod"));
```
세 번쨰 방법은 Delegate를 이용하여 패치하는 방법입니다.
```csharp
Patcher.AddPatch(PatchType.PatchMethod);
```
이 방법 말고 Attribute를 매서드에 추가하지 않고 패치하는 방법도 있습니다.
```csharp
Patcher.AddPatch(new JAPatchAttribute(typeof(scnGame), "Play", PatchType.Postfix, false) {
    Method = typeof(PatchType).Method("PatchMethod")
}));
```
더 간단한 방법으로는
```csharp
Patcher.AddPatch(typeof(PatchType).Method("PatchMethod"), new JAPatchAttribute(typeof(scnGame), "Play", PatchType.Postfix, false));
```
Delegate를 이용하여 패치하는 방법은 다음과 같습니다.
```csharp
Patcher.AddPatch(PatchType.PatchMethod, new JAPatchAttribute(typeof(scnGame), "Play", PatchType.Postfix, false));
```

패치를 추가하셨다면 마지막으로 패치를 진행하시면 됩니다.
```csharp
Patcher.Patch();
```

## Prefix
Prefix는 메서드 실행 전에 실행됩니다.

밑에 코드를 보시면 더 이해하기 쉬울 것입니다.
```csharp
public void OriginalMethod() {
    if(!PatchMethod()) return;
    OriginalCode();
}
```
Prefix는 다음과 같이 작동하게 됩니다.

Prefix의 반환값은 void 또는 bool입니다. bool일 경우 false를 반환하면 원래 메서드가 실행되지 않습니다.

자세한 내용은 [이 문서](https://harmony.pardeike.net/articles/patching-prefix.html)를 참고해주세요.

## Postfix
Postfix는 메서드 실행 후에 실행됩니다.

밑에 코드를 보시면 더 이해하기 쉬울 것입니다.
```csharp
public void OriginalMethod() {
    OriginalCode();
    PatchMethod();
}
```
Postfix는 다음과 같이 작동하게 됩니다.

Postfix의 반환값은 void입니다.

자세한 내용은 [이 문서](https://harmony.pardeike.net/articles/patching-postfix.html)를 참고해주세요.

## Transpiler
Transpiler는 메서드 실행 코드를 변경합니다.

밑에 코드를 보시면 더 이해하기 쉬울 것입니다.
```csharp
public void OriginalMethod() {
    OriginalCode1();
    OriginalCode2();
    //OriginalCode3();
    CreatedCode();
    OriginalCode4();
}
```
Transpiler는 다음과 같이 작동하게 됩니다.

Transpiler의 반환값은 IEnumerable<CodeInstruction>입니다.

Transpiler는 IL 코드를 변경하기 때문에 IL 코드를 알아야 합니다.

자세한 내용은 [이 문서](https://harmony.pardeike.net/articles/patching-transpiler.html)를 참고해주세요.

## Finalizer
Finalizer는 메서드 실행중 Exception이 발생하면 실행됩니다.

밑에 코드를 보시면 더 이해하기 쉬울 것입니다.
```csharp
public void OriginalMethod() {
    try {
        OriginalCode();
    } catch(Exception e) {
        throw PatchMethod(e);
    }
}
```
Finalizer는 다음과 같이 작동하게 됩니다.

Finalizer의 반환값은 Exception입니다.

Finalizer는 Exception을 잡아서 다시 던집니다.

자세한 내용은 [이 문서](https://harmony.pardeike.net/articles/patching-finalizer.html)를 참고해주세요.

## Replace
Replace는 메서드 전체 코드를 해당 코드로 변경합니다.

밑에 코드를 보시면 더 이해하기 쉬울 것입니다.
```csharp
public void OriginalMethod() {
    PatchMethodCode1();
    PatchMethodCode2();
    PatchMethodCode3();
    PatchMethodCode4();
    //OriginalCode1();
    //OriginalCode2();
    //OriginalCode3();
    //OriginalCode4();
}
```
Replace는 다음과 같이 작동하게 됩니다.

Replace의 반환값은 원래 매서드의 반환값과 같아야 됩니다.

Replace는 다음과 같이 사용할 수 있습니다.
```csharp
public class OriginalType {
    private int test = 2;

    public string OriginalMethod(bool a, int b, string c) {
        OriginalCode1(a);
        test = OriginalCode2(b);
        OriginalCode3(c);
        return OriginalCode4();
    }
}

[JAPatch(typeof(OriginalType), "OriginalMethod", PatchType.Replace, false)]
private static string PatchMethod(int b, bool a, OriginalType __instance, string c, int ___test, object[] __args) {
    PatchMethodCode1(a);
    ___test = PatchMethodCode2(b);
    PatchMethodCode3(c);
    return "Patched String";
}
```
Replace에서는 __instance와 ___변수를 사용할 수 있습니다.

___변수는 public, private 상관없이 가져올 수 있으며 ref 형식으로 가져오지 않고 값을 설정해도 전역변수의 값이 변경됩니다.

object[] __args를 통해 인자 모든값을 받아올 수 있습니다.

인자의 순서는 원래 매서드의 인자 순서가 달라도, 아에 원래있던 인자를 넣지 않아도 상관없습니다.

## [다음](Reflection.md)
