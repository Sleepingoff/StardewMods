# Buildings Tiled Framework

Tiled `.tmx` 파일을 이용해 Stardew Valley의 건물을 런타임에 덮어쓰는 SMAPI 모드입니다.

이 프레임워크는 Content Patcher 방식으로 `Data/Buildings`를 직접 수정하지 않습니다.
대신 의존 content pack에서 TMX 정의를 읽어와 Harmony와 런타임 데이터 주입으로 적용합니다.

## 주요 기능

- TMX 기반 런타임 건물 정의 로드
- 런타임 텍스처 교체
- 오브젝트 레이어 기반 액션 타일
- 런타임 `HumanDoor` 오버라이드
- 런타임 `AnimalDoor` 오버라이드
- TMX 기반 `CollisionMap` 생성
- 계절별 건물 변형
- 여러 content pack이 같은 건물을 덮을 때 GMCM 선택 지원

## 변경 내역

### 2026-04-01

- `.spring_outdoorsTileSheet` 같은 `.` 접두 게임 에셋 이름에 대해 현재 계절 기준 타일셋 탐색 추가
- 게임 내 계절이 바뀌면 자동으로 다시 로드하도록 변경
- 뒤집힌 TMX 타일의 flip 플래그 파싱 및 렌더링 지원 추가
- 필요할 때 실제 로드된 텍스처 폭 기준으로 tileset columns를 다시 계산하도록 수정
- 지원하지 않는 외부 tileset 참조는 전체 팩 로드를 실패시키지 않고 경고 후 스킵하도록 변경
- `Buildings` 레이어를 타일 단위 depth로 그리도록 바꿔 개별 건물 타일 정렬 개선

### 2026-03-31

- `Greenhouse_prev.tmx`는 온실 해금 전, `Greenhouse.tmx`는 해금 후에 사용하도록 진행도 분기 추가
- `AllowsFlooringUnderneath`, `DrawShadow`, `BuildCost`, `BuildDays`, `BuildMaterials`, `Builder` TMX 루트 속성 지원 추가
- 바닐라 `Data/Buildings`에 없는 ID에 대해 synthetic 런타임 `BuildingData` 주입 추가
- synthetic 건물의 빌더 분기 추가: `Wizard`는 마법사 메뉴, `Robin` 또는 미지정은 로빈 메뉴
- 오브젝트 레이어 `TouchAction` 파싱 및 `GameLocation.performTouchAction` 호출 지원 추가
- `TouchAction` 집중 추적 로그 추가 및 누락된 `UpdateWhenCurrentLocation` Harmony 등록 수정
- 게임 실행 중 TMX를 다시 읽을 수 있는 `btf_reload` 콘솔 커맨드 추가
- `.AssetName.png` 형태 타일셋 이미지에 대해 게임 자산 우선 로드 후 content pack 경로 폴백 지원
- 건축 메뉴 프리뷰가 현재 TMX 합성 결과를 쓰도록 개선
- `Back` 레이어 및 추가 draw layer의 프리뷰 합성 지원
- 저장 로드 중 `Farm`이 아직 없을 때 온실 진행도 조회로 크래시 나던 문제 수정
- 유니코드 폴더명이 포함된 content pack 경로 비교 보정
- footprint 정규화 과정에서 `TouchAction`이 사라지던 문제 수정

## Content Pack 구조

이 모드를 `ContentPackFor`로 지정한 content pack만 스캔합니다.

```text
[YourPack]/
├── manifest.json
├── Assets/
│   └── Farmhouse.png
└── Buildings/
    ├── Coop.tmx
    ├── Barn.tmx
    ├── Farmhouse.tmx
    ├── Cabin.tmx
    ├── spring/
    │   └── Farmhouse.tmx
    ├── summer/
    │   └── Farmhouse.tmx
    ├── fall/
    │   └── Farmhouse.tmx
    └── winter/
        └── Farmhouse.tmx
```

## manifest 예시

```json
{
  "Name": "Example Building Pack",
  "Author": "YourName",
  "Version": "1.0.0",
  "Description": "Runtime building overrides for Buildings Tiled Framework.",
  "UniqueID": "YourName.ExampleBuildingPack",
  "ContentPackFor": {
    "UniqueID": "slO.BuildingsTiledFramework"
  }
}
```

## 파일 이름 규칙

TMX 루트 속성에 `id`가 없으면 파일 이름을 건물 ID로 사용합니다.

예:

- `Coop.tmx` -> `Coop`
- `Barn.tmx` -> `Barn`
- `Farmhouse.tmx` -> `FarmHouse`
- `Cabin.tmx` -> `Cabin`

숫자 접미사가 있으면 업그레이드 단계 순서로 처리합니다.

예:

- `Coop.tmx`
- `Coop1.tmx`
- `Coop2.tmx`
- `Coop3.tmx`

이 경우 숫자 오름차순으로 로드됩니다.

진행도 이전 상태는 `_prev` 접미사를 사용할 수 있습니다.

예:

- `Greenhouse_prev.tmx` -> 온실 해금 전 사용
- `Greenhouse.tmx` -> 온실 해금 후 사용

## 계절별 변형

계절별 TMX는 네 계절 폴더에 모두 있어야 합니다.

```text
Buildings/spring/Farmhouse.tmx
Buildings/summer/Farmhouse.tmx
Buildings/fall/Farmhouse.tmx
Buildings/winter/Farmhouse.tmx
```

한 계절이라도 빠져 있으면 그 계절 세트는 건너뜁니다.

## 지원하는 TMX 범위

지원:

- 직교(Orthogonal) 맵
- CSV 타일 레이어 데이터
- TMX 내부에 직접 선언된 inline tileset
- `Action`, `TouchAction`, `Role` 속성이 있는 오브젝트 레이어
- 아래에 설명한 루트 속성

비지원:

- 무한 맵
- `<tileset source="...">` 형태의 외부 TSX tileset
- `.` 게임 에셋 참조가 아닌 content pack 외부 타일셋 이미지 경로
- Tiled 전체 기능 전부
- 임의의 바닐라 `BuildingData` 완전 대체

## 루트 속성

`<map><properties>` 아래에 넣습니다.

선택 속성:

- `id`
  - 명시적인 건물 ID
- `doorX`
  - 레거시 `HumanDoor` X 타일
- `doorY`
  - 레거시 `HumanDoor` Y 타일
- `exitX`
  - 사람 출입문 퇴장 처리에 쓰는 바깥쪽 출구 X 타일
- `exitY`
  - 사람 출입문 퇴장 처리에 쓰는 바깥쪽 출구 Y 타일
- `maxOccupants`
  - 런타임 `BuildingData.MaxOccupants`
- `AllowsFlooringUnderneath`
  - 런타임 `BuildingData.AllowsFlooringUnderneath`
- `DrawShadow`
  - `Buildings` 레이어 아래에 뒤집힌 그림자를 그릴지 여부
  - 기본값: `false`
- `BuildCost`
  - 해당 ID가 게임의 `Data/Buildings`에 없을 때 synthetic 런타임 항목의 건축 비용으로 사용
- `BuildDays`
  - 해당 ID가 게임의 `Data/Buildings`에 없을 때 synthetic 런타임 항목의 건축 일수로 사용
- `BuildMaterials`
  - 해당 ID가 게임의 `Data/Buildings`에 없을 때 synthetic 런타임 항목의 재료로 사용
  - 형식: `itemId amount itemId amount`
- `Builder`
  - 해당 ID가 게임의 `Data/Buildings`에 없을 때 어느 건축 메뉴에 넣을지 결정
  - 지원 값: `Wizard`, `Robin`, `null`

예시:

```xml
<properties>
  <property name="id" value="Barn"/>
  <property name="doorX" type="int" value="1"/>
  <property name="doorY" type="int" value="3"/>
  <property name="exitX" type="int" value="64"/>
  <property name="exitY" type="int" value="15"/>
  <property name="maxOccupants" type="int" value="8"/>
  <property name="AllowsFlooringUnderneath" type="bool" value="true"/>
  <property name="DrawShadow" type="bool" value="true"/>
  <property name="BuildCost" type="int" value="25000"/>
  <property name="BuildDays" type="int" value="2"/>
  <property name="BuildMaterials" type="string" value="(O)388 300 (O)390 100"/>
  <property name="Builder" type="string" value="Wizard"/>
</properties>
```

가능하면 `doorX` / `doorY`보다 `Role=HumanDoor` 사용을 권장합니다.

건물 ID가 게임의 `Data/Buildings`에 없으면, 이 프레임워크는 새 건물로 판단하고 런타임에 synthetic `BuildingData`를 주입합니다.

- `BuildCost`는 건축 메뉴에 표시되는 골드 비용을 결정합니다
- `BuildDays`는 공사 기간(일수)을 결정합니다
- `DrawShadow`는 기본값이 `false`이며, `true`일 때만 그림자를 그립니다
- `BuildMaterials`는 `itemId amount` 쌍으로 재료를 설정합니다
- `Builder=Wizard`는 마법사 건축 메뉴로 들어갑니다
- `Builder=Robin`, `Builder=null`, 또는 미지정은 로빈 상점으로 들어갑니다

## 타일셋 이미지

타일셋 이미지는 TMX의 `<tileset><image source="...">` 경로를 기준으로 읽습니다.

예시:

```xml
<tileset firstgid="1" name="Barn" tilewidth="16" tileheight="16" tilecount="64" columns="16">
  <image source="../../Assets/Barn.png" width="256" height="64"/>
</tileset>
```

경로는 TMX 파일 기준 상대 경로로 해석되며, content pack 내부에 있어야 합니다.

마지막 파일명이 `.`으로 시작하면, 로더는 먼저 앞의 `.`과 확장자를 뺀 이름으로 게임 자산을 찾습니다.

예:

- `<image source="../../Assets/.Greenhouse.png" .../>` -> 먼저 게임 자산 `Greenhouse` 시도
- 게임 자산을 로드하지 못하면 SMAPI 경고를 남기고 원래 content pack 경로로 폴백

에셋 이름이 `spring`, `summer`, `fall`, `winter` 중 하나로 시작하면 현재 계절 이름으로 먼저 바꿔 시도합니다.

예:

- `.spring_outdoorsTileSheet` -> 여름에는 먼저 `summer_outdoorsTileSheet` 시도
- `.spring_town` -> 겨울에는 먼저 `winter_town` 시도

TMX가 외부 TSX tileset이나 `.` 게임 에셋 참조가 아닌 content pack 외부 이미지 경로를 사용하면, 해당 TMX는 로드하지 않고 경고만 남깁니다.

## 타일 레이어

레이어 그룹은 다음 순서로 판정합니다.

1. 레이어 이름이 `Back`, `Buildings`, `Front`, `AlwaysFront`면 그 이름을 그대로 사용
2. 아니면 선택 속성 `Draw` 값을 사용
3. 둘 다 아니면 원래 레이어 이름을 그대로 사용

지원 draw 그룹:

- `Back`
- `Buildings`
- `Front`
- `AlwaysFront`

예시:

```xml
<layer name="Base">
  <properties>
    <property name="Draw" type="string" value="Buildings"/>
  </properties>
</layer>

<layer name="RoofFront">
  <properties>
    <property name="Draw" type="string" value="Front"/>
  </properties>
</layer>

<layer name="TreeTop">
  <properties>
    <property name="Draw" type="string" value="AlwaysFront"/>
  </properties>
</layer>
```

## 충돌 규칙

충돌은 `Buildings`로 분류된 레이어에서만 생성됩니다.

규칙:

- `gid == 0` -> 통과 가능
- `gid > 0` -> 충돌
- `Back`, `Front`, `AlwaysFront`는 충돌을 추가하지 않음
- 런타임 액션 타일은 다시 통과 가능으로 비움
- `HumanDoor`는 다시 통과 가능으로 비움
- `AnimalDoor`는 다시 통과 가능으로 비움

최종 결과는 런타임 `BuildingData.CollisionMap`에 주입됩니다.

## 배치 발판

배치 및 이동 발판은 다음 레이어 이름에서만 점유 타일을 계산합니다.

- `Back`
- `Buildings`

`Front`와 `AlwaysFront`는 배치/이동 범위를 넓히지 않습니다.

## 콘솔 커맨드

- `btf_reload`
  - 게임 실행 중 owned content pack의 TMX 정의를 다시 읽습니다
  - `Data/Buildings`와 프리뷰 캐시를 무효화합니다
  - 저장이 로드된 상태라면 이미 배치된 건물도 다시 바인딩합니다
