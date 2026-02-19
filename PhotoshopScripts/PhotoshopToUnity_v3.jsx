/*/
PhotoshopToUnity
psd 파일을 unity 의 prefab 으로 변환해주는 스크립트
Readme: 
Version:
v1, 2021/12/20
v2, 2022/01/10
 - 가이드 문서 수정
Author: Gyungmun (gyungmun.jeon@sundaytoz.com)
//*/


#target photoshop
app.bringToFront();

var scriptVersion = 1;
var cID = charIDToTypeID, sID = stringIDToTypeID;
var originalDoc, settings, progress, cancel, errors;
try {
	originalDoc = activeDocument;
} catch (ignored) {}

// 초성 - 가(ㄱ), 날(ㄴ) 닭(ㄷ)
var arrChoSung = [ 0x3131, 0x3132, 0x3134, 0x3137, 0x3138,
		0x3139, 0x3141, 0x3142, 0x3143, 0x3145, 0x3146, 0x3147, 0x3148,
		0x3149, 0x314a, 0x314b, 0x314c, 0x314d, 0x314e ];
// 중성 - 가(ㅏ), 야(ㅑ), 뺨(ㅑ)
var arrJungSung = [ 0x314f, 0x3150, 0x3151, 0x3152,
		0x3153, 0x3154, 0x3155, 0x3156, 0x3157, 0x3158, 0x3159, 0x315a,
		0x315b, 0x315c, 0x315d, 0x315e, 0x315f, 0x3160, 0x3161, 0x3162,
		0x3163 ];
// 종성 - 가(없음), 갈(ㄹ) 천(ㄴ)
var arrJongSung = [ 0x0000, 0x3131, 0x3132, 0x3133,
		0x3134, 0x3135, 0x3136, 0x3137, 0x3139, 0x313a, 0x313b, 0x313c,
		0x313d, 0x313e, 0x313f, 0x3140, 0x3141, 0x3142, 0x3144, 0x3145,
		0x3146, 0x3147, 0x3148, 0x314a, 0x314b, 0x314c, 0x314d, 0x314e ];

// 초성 - 가(ㄱ), 날(ㄴ) 닭(ㄷ)
var arrChoSungEng = [ "r", "R", "s", "e", "E",
	"f", "a", "q", "Q", "t", "T", "d", "w",
	"W", "c", "z", "x", "v", "g" ];

// 중성 - 가(ㅏ), 야(ㅑ), 뺨(ㅑ)
var arrJungSungEng = [ "k", "o", "i", "O",
	"j", "p", "u", "P", "h", "hk", "ho", "hl",
	"y", "n", "nj", "np", "nl", "b", "m", "ml",
	"l" ];

// 종성 - 가(없음), 갈(ㄹ) 천(ㄴ)
var arrJongSungEng = [ "", "r", "R", "rt",
	"s", "sw", "sg", "e", "f", "fr", "fa", "fq",
	"ft", "fx", "fv", "fg", "a", "q", "qt", "t",
	"T", "d", "w", "c", "z", "x", "v", "g" ];

// 단일 자음 - ㄱ,ㄴ,ㄷ,ㄹ... (ㄸ,ㅃ,ㅉ은 단일자음(초성)으로 쓰이지만 단일자음으론 안쓰임)
var arrSingleJaumEng = [ "r", "R", "rt",
	"s", "sw", "sg", "e","E" ,"f", "fr", "fa", "fq",
	"ft", "fx", "fv", "fg", "a", "q","Q", "qt", "t",
	"T", "d", "w", "W", "c", "z", "x", "v", "g" ];

main();
function main()
{
    showSettingsDialog();
//	renameLayers(app.activeDocument);
}

// 이름 변경
function renameLayers(ref)
{
	var len = ref.layers.length;

	for (var i = len - 1; i >= 0; i--)
	{
		var layer = ref.layers[i];
		if(layer.visible == true)
		{
			rename(layer);
		}
	}

	function rename(inLayer)
	{
		if (inLayer.typename == 'LayerSet')
		{
			renameLayers(inLayer);
		}
		else
		{
			var prefix = "img_";
			var suffix = "";
            var layerName = inLayer.name.toLowerCase();

			// 모든 TEXT 레이어를 txt_로 통합 처리 (stxt_ 제거)
			if(inLayer.kind == LayerKind.TEXT)
			{
				prefix = "txt_";

				// 이미 txt_ 또는 stxt_로 시작하면 스킵
				if(layerName.indexOf("txt_") != -1 || layerName.indexOf("stxt_") != -1)
				{
					return;
				}

				// 레이어 이름의 공백을 _ 로 변경
                inLayer.name = inLayer.name.replace(/\s/gi, "_");

				// 한글을 영문으로 변경
				inLayer.name = convertKorToEng(inLayer.name);

				var text = inLayer.textItem;
				var stroke = getStroke(text);
				var strokeSize = "null";
				var strokeColor = "null";
				var dropShadow = getDropShadow(text);
				var dropShadowLocalLightingAngle = "null";
				var dropShadowOpacity = "null";
				var dropShadowDistance = "null";
				var dropShadowColor = "null";

				if(stroke)
				{
					strokeSize = stroke.size;
					strokeColor = stroke.color;
				}
				if(dropShadow)
				{
					dropShadowLocalLightingAngle = dropShadow.localLightingAngle;
					dropShadowDistance = dropShadow.distance;
					dropShadowOpacity = dropShadow.opacity;
					dropShadowColor = dropShadow.color;
				}

				var textExtents = getTextExtents(text);

				// 텍스트 - 이름,폰트명,폰트 사이즈,폰트 내용,색
				suffix += '^';
				suffix += text.font + '^';
				suffix += Math.round(text.size * textExtents.x_scale) + '^';
				suffix += text.contents.replace(/(\r\n|\n|\r)/gm, "<br>") + '^';
				suffix += '#' + text.color.rgb.hexValue;

				// 이펙트 - stroke
				suffix += '^';
				suffix += strokeSize + '^';
				if(strokeColor != "null")
				{
					suffix += '#' + strokeColor;
				}
				else
				{
					suffix += strokeColor;
				}

				// 이펙트 - dropShadow
				suffix += '^';
				suffix += dropShadowLocalLightingAngle + '^';
				suffix += dropShadowDistance + '^';
				suffix += dropShadowOpacity + '^';
				if(dropShadowColor != "null")
				{
					suffix += '#' + dropShadowColor;
				}
				else
				{
					suffix += dropShadowColor;
				}

				inLayer.name = prefix + inLayer.name + suffix;
			}
            // 원래는 img_ 를 추가하던 부분이지만 이미지 같은 경우 레이어명을 변경 안하기로 함
//			else if(layerName.indexOf("nine_") == -1 && 
//				layerName.indexOf("cell_") == -1 && 
//				layerName.indexOf("table_") == -1 && 
//				layerName.indexOf("btn_") == -1 &&
//                layerName.indexOf("ex_") == -1 &&
//                layerName.indexOf("img_") == -1 &&
//                layerName.indexOf("stxt_") == -1 &&
//				layerName.indexOf(prefix) == -1)
//			{
//				// 레이어 이름의 공백을 _ 로 변경
//				inLayer.name = inLayer.name.replace(/\s/gi, "_");
//
//				// 한글을 영문으로 변경
//				inLayer.name = convertKorToEng(inLayer.name);
//
//				inLayer.name = prefix + inLayer.name + suffix;
//			}

//			app.preferences.rulerUnits = Units.PIXELS;  
//			var bounds = app.activeDocument.activeLayer.bounds;  
//			var layerWidth = bounds[2].as('px')-bounds[0].as('px');  
//			var layerHeight = bounds[3].as('px')-bounds[1].as('px');  
//			alert(layerHeight);
		}
	}
}

// Stroke 의 size 와 컬러 값을 가지고 옴
function getStroke(text_item)
{
    app.activeDocument.activeLayer = text_item.parent;

	var ref = new ActionReference();
	ref.putEnumerated( charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt") );
	var desc = executeActionGet(ref);
	if(desc.hasKey(stringIDToTypeID( 'layerEffects' ))){
		if(!desc.getBoolean (stringIDToTypeID( 'layerFXVisible'))) return undefined;
		desc = desc.getObjectValue(stringIDToTypeID('layerEffects'));
		if(!desc.hasKey(stringIDToTypeID( 'frameFX'))) return null;
		desc = desc.getObjectValue(stringIDToTypeID('frameFX'));
		if(!desc.getBoolean(stringIDToTypeID( 'enabled'))) return null;

		var strokeStyleColor = desc.getObjectValue(stringIDToTypeID("color"));
		var tmpC = new SolidColor();
		tmpC.rgb.red = strokeStyleColor.getDouble(charIDToTypeID("Rd  "));
		tmpC.rgb.green = strokeStyleColor.getDouble(charIDToTypeID("Grn "));
		tmpC.rgb.blue = strokeStyleColor.getDouble(charIDToTypeID("Bl  "));

		return {
			size: desc.getUnitDoubleValue (stringIDToTypeID( 'size' )),
			color: tmpC.rgb.hexValue
		}
	}
}

// DropShadow 값을 가져옴
function getDropShadow(text_item)
{
    app.activeDocument.activeLayer = text_item.parent;

	var ref = new ActionReference();
	ref.putEnumerated( charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt") );
	var desc = executeActionGet(ref);
	if(desc.hasKey(stringIDToTypeID( 'layerEffects' ))){
		if(!desc.getBoolean (stringIDToTypeID( 'layerFXVisible'))) return undefined;
		desc = desc.getObjectValue(stringIDToTypeID('layerEffects'));
		if(!desc.hasKey(stringIDToTypeID( 'dropShadow'))) return null;
		desc = desc.getObjectValue(stringIDToTypeID('dropShadow'));
		if(!desc.getBoolean(stringIDToTypeID( 'enabled'))) return null;

		var dropShadowStyleColor = desc.getObjectValue(stringIDToTypeID("color"));
		var tmpC = new SolidColor();
		tmpC.rgb.red = dropShadowStyleColor.getDouble(charIDToTypeID("Rd  "));
		tmpC.rgb.green = dropShadowStyleColor.getDouble(charIDToTypeID("Grn "));
		tmpC.rgb.blue = dropShadowStyleColor.getDouble(charIDToTypeID("Bl  "));

		return {
			localLightingAngle: desc.getUnitDoubleValue (stringIDToTypeID( 'localLightingAngle' )),
			distance: desc.getUnitDoubleValue (stringIDToTypeID( 'distance' )),
			opacity: desc.getUnitDoubleValue (stringIDToTypeID( 'opacity' )),
			color: tmpC.rgb.hexValue
		}
	}
}

// 위치와 사이즈를 가지고 오기 위함
function getTextExtents(text_item)
{
    app.activeDocument.activeLayer = text_item.parent;
    var ref = new ActionReference();
    ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
    var desc = executeActionGet(ref).getObjectValue(stringIDToTypeID('textKey'));
    var bounds = desc.getObjectValue(stringIDToTypeID('bounds'));
    var width = bounds.getUnitDoubleValue(stringIDToTypeID('right'));
    var height = bounds.getUnitDoubleValue(stringIDToTypeID('bottom'));
    var x_scale = 1;
    var y_scale = 1;
    if (desc.hasKey(stringIDToTypeID('transform')))
	{
        var transform = desc.getObjectValue(stringIDToTypeID('transform'));
        x_scale = transform.getUnitDoubleValue(stringIDToTypeID('xx'));
        y_scale = transform.getUnitDoubleValue(stringIDToTypeID('yy'));
    }

    return {
        x: Math.round(text_item.position[0]),
        y: Math.round(text_item.position[1]),
        width: Math.round(width * x_scale),
        height: Math.round(height * y_scale),
        x_scale: x_scale,
        y_scale: y_scale
    }
}

// 한글을 영문으로 변경함. 한글 외에는 무시
function convertKorToEng(word)
{
//	var word 		= "아이템 보너스 (+10%)";		// 분리할 단어
	var result		= "";									// 결과 저장할 변수
	var resultEng	= "";									// 알파벳으로
	
    // 10자 넘어가면 잘라버림
    var MAX_LAYER_NAME_LEN = 15;
    if(word.length > MAX_LAYER_NAME_LEN)
    {
        word = word.substring(0, MAX_LAYER_NAME_LEN - 1);
    }

	for (var i = 0; i < word.length; i++)
	{
		var chars = (word.charCodeAt(i) - 0xAC00);
		if (chars >= 0 && chars <= 11172)
		{
			// A-1. 초/중/종성 분리
			var chosung 	= parseInt(chars / (21 * 28));
			var jungsung 	= parseInt(chars % (21 * 28) / 28);
			var jongsung 	= parseInt(chars % (21 * 28) % 28);

			// A-2. result에 담기
			result = result + arrChoSung[chosung] + arrJungSung[jungsung];
			
			// 자음분리
			if(jongsung != 0x0000)
			{
				// A-3. 종성이 존재할경우 result에 담는다
				result =  result + arrJongSung[jongsung];
			}

			// 알파벳으로
			resultEng = resultEng + arrChoSungEng[chosung] + arrJungSungEng[jungsung];
			if(jongsung != 0x0000) 
			{
				// A-3. 종성이 존재할경우 result에 담는다
				resultEng =  resultEng + arrJongSungEng[jongsung];
			}
		}
		else
		{
			// B. 한글이 아니거나 자음만 있을경우

			// 자음분리
			result = result + (chars + 0xAC00);
			
			// 알파벳으로
			if(chars >= 34097 && chars <= 34126)
			{
				// 단일자음인 경우
				var jaum 	= parseInt(chars - 34097);
				resultEng = resultEng + arrSingleJaumEng[jaum];
			}
			else if(chars >= 34127 && chars <= 34147)
			{
				// 단일모음인 경우
				var moum 	= parseInt(chars - 34127);
				resultEng = resultEng + arrJungSungEng[moum];
			}
			else
			{
				// 한글이 아닐 경우
				resultEng = resultEng + word.charAt(i);
			}
		}
	}

//	alert(word);
//	alert(result);
//	alert(resultEng);

	return resultEng;
}

// 셋팅 팝업
function showSettingsDialog () {
	if (parseInt(app.version) < 22) {
		alert("Photoshop 2021 버전을 사용하시기 바랍니다.");
		return;
	}
	if (!originalDoc) {
		alert("스크립트를 실행하기 전에 psd 파일을 먼저 열어주세요.");
		return;
	}
	try {
		decodeURI(activeDocument.path);
	} catch (e) {
		alert("스크립트를 실행하기 전에 psd 파일을 먼저 열어주세요.");
		return;
	}

	// Layout.
	var dialog, group;
	try {
		dialog = new Window("dialog", "PhotoshopToUnity v" + scriptVersion);
	} catch (e) {
		throw new Error("\n\n알 수 없는 원인으로 스크립트를 실행할 수 없습니다. 포토샵을 다시 켜주세요.\n\n" + e.message);
	}
	dialog.alignChildren = "fill";

	var settingsGroup = dialog.add("panel", undefined, "유효한 접두사 리스트");
		settingsGroup.margins = [10,15,10,10];
		settingsGroup.alignChildren = "fill";
            var helpText = settingsGroup.add("statictext", undefined, ""
            + "• btn_ -> 버튼\n"
            + "• icon_ -> 아이콘(이미지)\n"
            + "• deco_ -> 데코 이미지(특정한 이미지 패턴을 반복 할 때 복사해서 사용)\n"
            + "• img_ -> UI 이미지(게이지, 자주 사용하는 이미지)\n"
            + "• bg_ -> BG,타이틀 이미지\n"
            + "• inner_ -> 팝업 이너\n"
            + "• item_ -> 아이템 이미지\n"
            + "• txt_ -> TextMeshPro 텍스트 (RBTextMeshProUGUI)\n"
            , {multiline: true});
	        helpText.preferredSize.width = 325;

	var buttonGroup = dialog.add("group");
		group = buttonGroup.add("group");
			group.alignment = ["fill", ""];
			group.alignChildren = ["center", ""];
			var runButton = group.add("button", undefined, "OK");
			var cancelButton = group.add("button", undefined, "Cancel");

	// 이벤트
	cancelButton.onClick = function () {
		cancel = true;
		dialog.close();
		return;
	};

	function updateSettings () {
		settings.common = commonCheckbox.value;
	}

	runButton.onClick = function () {
		runButton.enabled = false;
		cancelButton.enabled = false;

		var rulerUnits = app.preferences.rulerUnits;
		app.preferences.rulerUnits = Units.PIXELS;
		try {
			renameLayers(app.activeDocument);
		} catch (e) {
			if (e.message == "User cancelled the operation") return;
			alert("An unexpected error has occurred:\n\n[line " + e.line + "] " + e.message + "\n\nTo debug, run the PhotoshopToUnity script using Adobe ExtendScript "
				+ "with \"Debug > Do not break on guarded exceptions\" unchecked.");
			debugger;
		} finally {
			if (activeDocument != originalDoc) activeDocument.close(SaveOptions.DONOTSAVECHANGES);
			app.preferences.rulerUnits = rulerUnits;
			if (progress && progress.dialog) progress.dialog.close();
			dialog.close();
		}
	};

	dialog.center();
	dialog.show();
}