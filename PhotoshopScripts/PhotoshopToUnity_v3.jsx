/*/
PhotoshopToUnity
psd 파일을 unity 의 prefab 으로 변환해주는 스크립트
Version:
v1, 2021/12/20
v2, 2022/01/10 - 가이드 문서 수정
v3, 2026/02/19 - txt_/stxt_ 통합, 코드 정리
Author: Gyungmun (gyungmun.jeon@sundaytoz.com)
//*/

#target photoshop
app.bringToFront();

var scriptVersion = 3;
var originalDoc;
try {
	originalDoc = activeDocument;
} catch (ignored) {}

// 초성 로마자 (두벌식 기준)
var arrChoSungEng = [ "r", "R", "s", "e", "E",
	"f", "a", "q", "Q", "t", "T", "d", "w",
	"W", "c", "z", "x", "v", "g" ];

// 중성 로마자
var arrJungSungEng = [ "k", "o", "i", "O",
	"j", "p", "u", "P", "h", "hk", "ho", "hl",
	"y", "n", "nj", "np", "nl", "b", "m", "ml",
	"l" ];

// 종성 로마자
var arrJongSungEng = [ "", "r", "R", "rt",
	"s", "sw", "sg", "e", "f", "fr", "fa", "fq",
	"ft", "fx", "fv", "fg", "a", "q", "qt", "t",
	"T", "d", "w", "c", "z", "x", "v", "g" ];

// 단일 자음 로마자
var arrSingleJaumEng = [ "r", "R", "rt",
	"s", "sw", "sg", "e", "E", "f", "fr", "fa", "fq",
	"ft", "fx", "fv", "fg", "a", "q", "Q", "qt", "t",
	"T", "d", "w", "W", "c", "z", "x", "v", "g" ];

main();
function main()
{
    showSettingsDialog();
}

// 이름 변경
function renameLayers(ref)
{
	var len = ref.layers.length;

	for (var i = len - 1; i >= 0; i--)
	{
		var layer = ref.layers[i];
		if (layer.visible == true)
		{
			rename(layer);
		}
	}

	function rename(inLayer)
	{
		if (inLayer.typename == 'LayerSet')
		{
			renameLayers(inLayer);
			return;
		}

		var layerName = inLayer.name.toLowerCase();

		// 텍스트 레이어 처리 (txt_, stxt_ 통합)
		if (inLayer.kind == LayerKind.TEXT)
		{
			// 이미 처리된 레이어 스킵
			if (layerName.indexOf("txt_") != -1 || layerName.indexOf("stxt_") != -1)
			{
				return;
			}

			// 공백 → _, 한글 → 로마자
			inLayer.name = inLayer.name.replace(/\s/gi, "_");
			inLayer.name = convertKorToEng(inLayer.name);

			var text = inLayer.textItem;
			var xScale = getTextXScale(text);

			var stroke = getLayerEffect(text, 'frameFX');
			var dropShadow = getLayerEffect(text, 'dropShadow');

			// 텍스트 데이터: 폰트명^사이즈^내용^색상
			var suffix = '^'
				+ text.font + '^'
				+ Math.round(text.size * xScale) + '^'
				+ text.contents.replace(/(\r\n|\n|\r)/gm, "<br>") + '^'
				+ '#' + text.color.rgb.hexValue;

			// 스트로크
			suffix += '^';
			if (stroke)
			{
				suffix += stroke.size + '^#' + stroke.color;
			}
			else
			{
				suffix += 'null^null';
			}

			// 드롭섀도우
			suffix += '^';
			if (dropShadow)
			{
				suffix += dropShadow.localLightingAngle + '^'
					+ dropShadow.distance + '^'
					+ dropShadow.opacity + '^'
					+ '#' + dropShadow.color;
			}
			else
			{
				suffix += 'null^null^null^null';
			}

			inLayer.name = "txt_" + inLayer.name + suffix;
		}
	}
}

// 레이어 이펙트 공통 취득 (frameFX = stroke, dropShadow)
function getLayerEffect(text_item, effectKey)
{
	app.activeDocument.activeLayer = text_item.parent;

	var ref = new ActionReference();
	ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
	var desc = executeActionGet(ref);

	if (!desc.hasKey(stringIDToTypeID('layerEffects'))) return null;
	if (!desc.getBoolean(stringIDToTypeID('layerFXVisible'))) return null;

	desc = desc.getObjectValue(stringIDToTypeID('layerEffects'));
	if (!desc.hasKey(stringIDToTypeID(effectKey))) return null;

	desc = desc.getObjectValue(stringIDToTypeID(effectKey));
	if (!desc.getBoolean(stringIDToTypeID('enabled'))) return null;

	var colorDesc = desc.getObjectValue(stringIDToTypeID("color"));
	var c = new SolidColor();
	c.rgb.red   = colorDesc.getDouble(charIDToTypeID("Rd  "));
	c.rgb.green = colorDesc.getDouble(charIDToTypeID("Grn "));
	c.rgb.blue  = colorDesc.getDouble(charIDToTypeID("Bl  "));

	if (effectKey == 'frameFX')
	{
		return {
			size:  desc.getUnitDoubleValue(stringIDToTypeID('size')),
			color: c.rgb.hexValue
		};
	}
	else // dropShadow
	{
		return {
			localLightingAngle: desc.getUnitDoubleValue(stringIDToTypeID('localLightingAngle')),
			distance:           desc.getUnitDoubleValue(stringIDToTypeID('distance')),
			opacity:            desc.getUnitDoubleValue(stringIDToTypeID('opacity')),
			color:              c.rgb.hexValue
		};
	}
}

// 텍스트 x 스케일 취득 (폰트 사이즈 보정용)
function getTextXScale(text_item)
{
	app.activeDocument.activeLayer = text_item.parent;
	var ref = new ActionReference();
	ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
	var desc = executeActionGet(ref).getObjectValue(stringIDToTypeID('textKey'));

	if (desc.hasKey(stringIDToTypeID('transform')))
	{
		return desc.getObjectValue(stringIDToTypeID('transform'))
		           .getUnitDoubleValue(stringIDToTypeID('xx'));
	}
	return 1;
}

// 한글 → 두벌식 로마자 변환
function convertKorToEng(word)
{
	var MAX_LEN = 15;
	if (word.length > MAX_LEN) word = word.substring(0, MAX_LEN - 1);

	var result = "";
	for (var i = 0; i < word.length; i++)
	{
		var code = word.charCodeAt(i) - 0xAC00;

		if (code >= 0 && code <= 11172)
		{
			// 완성형 한글: 초/중/종성 분리
			var cho  = parseInt(code / (21 * 28));
			var jung = parseInt(code % (21 * 28) / 28);
			var jong = parseInt(code % 28);

			result += arrChoSungEng[cho] + arrJungSungEng[jung];
			if (jong != 0) result += arrJongSungEng[jong];
		}
		else if (code >= 34097 && code <= 34126)
		{
			// 단일 자음
			result += arrSingleJaumEng[code - 34097];
		}
		else if (code >= 34127 && code <= 34147)
		{
			// 단일 모음
			result += arrJungSungEng[code - 34127];
		}
		else
		{
			// 한글 외 문자 그대로
			result += word.charAt(i);
		}
	}
	return result;
}

// 설정 팝업
function showSettingsDialog()
{
	if (parseInt(app.version) < 22)
	{
		alert("Photoshop 2021 이상 버전을 사용해주세요.");
		return;
	}
	if (!originalDoc)
	{
		alert("스크립트를 실행하기 전에 psd 파일을 먼저 열어주세요.");
		return;
	}
	try {
		decodeURI(activeDocument.path);
	} catch (e) {
		alert("스크립트를 실행하기 전에 psd 파일을 먼저 열어주세요.");
		return;
	}

	var dialog;
	try {
		dialog = new Window("dialog", "PhotoshopToUnity v" + scriptVersion);
	} catch (e) {
		throw new Error("\n\n알 수 없는 원인으로 스크립트를 실행할 수 없습니다. 포토샵을 다시 켜주세요.\n\n" + e.message);
	}
	dialog.alignChildren = "fill";

	var settingsGroup = dialog.add("panel", undefined, "유효한 접두사 리스트");
	settingsGroup.margins = [10, 15, 10, 10];
	settingsGroup.alignChildren = "fill";
	var helpText = settingsGroup.add("statictext", undefined,
		  "• btn_  → 버튼\n"
		+ "• icon_ → 아이콘(이미지)\n"
		+ "• deco_ → 데코 이미지\n"
		+ "• img_  → UI 이미지\n"
		+ "• bg_   → BG / 타이틀 이미지\n"
		+ "• inner_→ 팝업 이너\n"
		+ "• item_ → 아이템 이미지\n"
		+ "• txt_  → TextMeshPro 텍스트 (자동 생성)",
		{multiline: true});
	helpText.preferredSize.width = 325;

	var buttonGroup = dialog.add("group");
	var group = buttonGroup.add("group");
	group.alignment = ["fill", ""];
	group.alignChildren = ["center", ""];
	var runButton    = group.add("button", undefined, "OK");
	var cancelButton = group.add("button", undefined, "Cancel");

	cancelButton.onClick = function () { dialog.close(); };

	runButton.onClick = function ()
	{
		runButton.enabled    = false;
		cancelButton.enabled = false;

		var rulerUnits = app.preferences.rulerUnits;
		app.preferences.rulerUnits = Units.PIXELS;
		try {
			renameLayers(app.activeDocument);
		} catch (e) {
			if (e.message == "User cancelled the operation") return;
			alert("An unexpected error has occurred:\n\n[line " + e.line + "] " + e.message
				+ "\n\nTo debug, run the script using Adobe ExtendScript"
				+ " with \"Debug > Do not break on guarded exceptions\" unchecked.");
			debugger;
		} finally {
			if (activeDocument != originalDoc) activeDocument.close(SaveOptions.DONOTSAVECHANGES);
			app.preferences.rulerUnits = rulerUnits;
			dialog.close();
		}
	};

	dialog.center();
	dialog.show();
}
