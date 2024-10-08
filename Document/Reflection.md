# JAMod 개발 가이드 - Reflection
### [목차로 이동](DevelopGuide.md)
1. [소개](#소개)
2. [Method](#Method)
3. [Field](#Field)
4. [Property](#Property)
5. [SetValue](#SetValue)
6. [GetValue](#GetValue)

## 소개
Reflection은 프로그램이 실행 중에 자신의 구조를 조사하고 조작할 수 있는 능력을 말합니다.

JALib에서는 Reflection을 쉽게할 수 있도록 도와주는 도구가 있습니다.

SimpleReflection을 사용하면 다음과 같은 Reflection을 쉽게 할 수 있습니다.

## Method
JALib을 통해 MethodInfo를 가져오는 방법입니다.
```csharp
MethodInfo method = typeof(TestType).Method("TestMethod");
```
해당 방법으로 매서드를 가져올 경우 해당 매서드가 public이 아니어도 별도의 코드 추가 없이 가져올 수 있습니다.

Type을 거치지 않고 객체를 통해 바로 가져올 수 있습니다.
```csharp
MethodInfo method = testObject.Method("TestMethod");
```

## Field
JALib을 통해 FieldInfo를 가져오는 방법입니다.
```csharp
FieldInfo field = typeof(TestType).Field("TestField");
```
해당 방법으로 필드를 가져올 경우 해당 필드가 public이 아니어도 별도의 코드 추가 없이 가져올 수 있습니다.

Type을 거치지 않고 객체를 통해 바로 가져올 수 있습니다.
```csharp
FieldInfo field = testObject.Field("TestField");
```

## Property
JALib을 통해 PropertyInfo를 가져오는 방법입니다.
```csharp
PropertyInfo property = typeof(TestType).Property("TestProperty");
```
해당 방법으로 프로퍼티를 가져올 경우 해당 프로퍼티가 public이 아니어도 별도의 코드 추가 없이 가져올 수 있습니다.

Type을 거치지 않고 객체를 통해 바로 가져올 수 있습니다.
```csharp
PropertyInfo property = testObject.Property("TestProperty");
```

## SetValue
JALib을 통해 Field의 값을 설정하는 방법입니다.
```csharp
field.SetValue(testObject, 10, 20);
```
이 방법으로 Property 또한 가능합니다.

또 FieldInfo를 거치지 않고도 값을 설정할 수 있습니다.
```csharp
testObject.SetValue("TestField", 10, 20);
```

## GetValue
JALib을 통해 Field의 값을 가져오는 방법입니다.
```csharp
int value = (int)field.GetValue(testObject);
```
이 방법으로 Property 또한 가능합니다.

또 FieldInfo를 거치지 않고도 값을 가져올 수 있습니다.
```csharp
int value = testObject.GetValue<int>("TestField");
```