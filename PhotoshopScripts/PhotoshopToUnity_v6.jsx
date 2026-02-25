/*/
photoshop_to_unity_v6.jsx
psd 파일을 unity 의 prefab 으로 변환해주는 스크립트
Version:
v1, 2021/12/20
v2, 2022/01/10 - 가이드 문서 수정
v3, 2026/02/19 - txt_/stxt_ 통합, 코드 정리
v4, 2026/02/25 - 클리핑 마스크 및 레이어 효과 병합 기능 추가, snake_case 일괄 적용
v5, 2026/02/25 - Bottom-up 탐색으로 인덱스 꼬임 방지
v6, 2026/02/25 - Action Manager 도입, 완벽한 Bottom-Up 래스터화 및 클리핑 마스크 병합 적용
//*/

#target photoshop
app.bringToFront();

var script_version = 6;
var original_doc;
try {
	original_doc = activeDocument;
} catch (ignored) {}

// 초성 로마자 (두벌식 기준)
var arr_cho_sung_eng = [ "r", "R", "s", "e", "E",
	"f", "a", "q", "Q", "t", "T", "d", "w",
	"W", "c", "z", "x", "v", "g" ];

// 중성 로마자
var arr_jung_sung_eng = [ "k", "o", "i", "O",
	"j", "p", "u", "P", "h", "hk", "ho", "hl",
	"y", "n", "nj", "np", "nl", "b", "m", "ml",
	"l" ];

// 종성 로마자
var arr_jong_sung_eng = [ "", "r", "R", "rt",
	"s", "sw", "sg", "e", "f", "fr", "fa", "fq",
	"ft", "fx", "fv", "fg", "a", "q", "qt", "t",
	"T", "d", "w", "c", "z", "x", "v", "g" ];

// 단일 자음 로마자
var arr_single_jaum_eng = [ "r", "R", "rt",
	"s", "sw", "sg", "e", "E", "f", "fr", "fa", "fq",
	"ft", "fx", "fv", "fg", "a", "q", "Q", "qt", "t",
	"T", "d", "w", "W", "c", "z", "x", "v", "g" ];

main();

function main() {
    show_settings_dialog();
}

// Action Manager: 레이어 래스터화 (모양 및 레이어 스타일 모두 포함)
function rasterize_layer(layer) {
    app.activeDocument.activeLayer = layer;
    
    // 1. 일반 래스터화 (모양, 스마트 오브젝트 등)
    try {
        var desc = new ActionDescriptor();
        var ref = new ActionReference();
        ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
        desc.putReference(charIDToTypeID("null"), ref);
        executeAction(stringIDToTypeID("rasterizeLayer"), desc, DialogModes.NO);
    } catch(e) {}

    // 2. 레이어 스타일 래스터화
    try {
        var desc2 = new ActionDescriptor();
        var ref2 = new ActionReference();
        ref2.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
        desc2.putReference(charIDToTypeID("null"), ref2);
        desc2.putEnumerated(stringIDToTypeID("what"), stringIDToTypeID("rasterizeItem"), stringIDToTypeID("layerStyle"));
        executeAction(stringIDToTypeID("rasterizeLayer"), desc2, DialogModes.NO);
    } catch(e) {}
}

// Action Manager: 아래 레이어와 병합 (Ctrl+E / 클리핑 마스크 병합)
function merge_down() {
    try {
        executeAction(charIDToTypeID("Mrg2"), undefined, DialogModes.NO);
    } catch(e) {}
}

// 사용자가 요청한 순서(Bottom-up 읽기 -> 래스터화 -> 마스크 병합)로 처리
function flatten_effects_and_masks(ref) {
    var i = ref.layers.length - 1;

    while (i >= 0) {
        var layer = ref.layers[i];

        if (layer.typename == 'LayerSet') {
            flatten_effects_and_masks(layer);
            i--;
            continue;
        }

        if (layer.typename == 'ArtLayer') {
            // 텍스트, 숨겨진 레이어, 배경 레이어는 패스
            if (layer.kind == LayerKind.TEXT || !layer.visible || layer.isBackgroundLayer) {
                i--;
                continue;
            }

            // 클리핑 마스크인 경우, 베이스 레이어를 찾을 때 함께 처리되므로 여기선 건너뜀
            if (layer.grouped) {
                i--;
                continue;
            }

            // 베이스 레이어(가장 아래에 있는 타겟 레이어) 발견
            var base_index = i;
            var base_name = layer.name;

            // [1] 베이스 레이어 먼저 래스터화
            rasterize_layer(ref.layers[base_index]);

            // [2] 바로 위에 클리핑 마스크들이 있는지 확인하고 순차적으로 병합 (Ctrl+E)
            while (base_index > 0 && ref.layers[base_index - 1].grouped) {
                // 마스크 레이어도 안전하게 래스터화
                rasterize_layer(ref.layers[base_index - 1]);

                // 마스크 레이어 선택 후 아래로 병합 (클리핑 마스크 병합)
                app.activeDocument.activeLayer = ref.layers[base_index - 1];
                merge_down();

                // 병합되면 두 레이어가 하나가 되면서 배열의 길이가 줄어듦
                // 새롭게 병합된 베이스 레이어의 인덱스는 기존 인덱스 - 1 이 됨
                base_index = base_index - 1;
            }

            // 원본 레이어 이름 복구 (병합 과정에서 이름이 마스크 이름으로 바뀌는 것 방지)
            if (base_index < ref.layers.length) {
                ref.layers[base_index].name = base_name;
            }

            // [3] 병합 완료 후 다음 탐색할 인덱스 갱신 (마스크들을 모두 건너뛴 위치)
            i = base_index - 1;
        } else {
            i--;
        }
    }
}

// 이름 변경 (기존 로직)
function rename_layers(ref) {
	var len = ref.layers.length;

	for (var i = len - 1; i >= 0; i--) {
		var layer = ref.layers[i];
		if (layer.visible == true) {
			rename_layer(layer);
		}
	}

	function rename_layer(in_layer) {
		if (in_layer.typename == 'LayerSet') {
			rename_layers(in_layer);
			return;
		}

		var layer_name = in_layer.name.toLowerCase();

		if (in_layer.kind == LayerKind.TEXT) {
			if (layer_name.indexOf("txt_") != -1 || layer_name.indexOf("stxt_") != -1) {
				return;
			}

			in_layer.name = in_layer.name.replace(/\s/gi, "_");
			in_layer.name = convert_kor_to_eng(in_layer.name);

			var text = in_layer.textItem;
			var x_scale = get_text_x_scale(text);

			var stroke = get_layer_effect(text, 'frameFX');
			var drop_shadow = get_layer_effect(text, 'dropShadow');

			var suffix = '^'
				+ text.font + '^'
				+ Math.round(text.size * x_scale) + '^'
				+ text.contents.replace(/(\r\n|\n|\r)/gm, "<br>") + '^'
				+ '#' + text.color.rgb.hexValue;

			suffix += '^';
			if (stroke) {
				suffix += stroke.size + '^#' + stroke.color;
			} else {
				suffix += 'null^null';
			}

			suffix += '^';
			if (drop_shadow) {
				suffix += drop_shadow.localLightingAngle + '^'
					+ drop_shadow.distance + '^'
					+ drop_shadow.opacity + '^'
					+ '#' + drop_shadow.color;
			} else {
				suffix += 'null^null^null^null';
			}

			in_layer.name = "txt_" + in_layer.name + suffix;
		}
	}
}

// 레이어 이펙트 공통 취득
function get_layer_effect(text_item, effect_key) {
	app.activeDocument.activeLayer = text_item.parent;

	var ref = new ActionReference();
	ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
	var desc = executeActionGet(ref);

	if (!desc.hasKey(stringIDToTypeID('layerEffects'))) return null;
	if (!desc.getBoolean(stringIDToTypeID('layerFXVisible'))) return null;

	desc = desc.getObjectValue(stringIDToTypeID('layerEffects'));
	if (!desc.hasKey(stringIDToTypeID(effect_key))) return null;

	desc = desc.getObjectValue(stringIDToTypeID(effect_key));
	if (!desc.getBoolean(stringIDToTypeID('enabled'))) return null;

	var color_desc = desc.getObjectValue(stringIDToTypeID("color"));
	var c = new SolidColor();
	c.rgb.red   = color_desc.getDouble(charIDToTypeID("Rd  "));
	c.rgb.green = color_desc.getDouble(charIDToTypeID("Grn "));
	c.rgb.blue  = color_desc.getDouble(charIDToTypeID("Bl  "));

	if (effect_key == 'frameFX') {
		return {
			size:  desc.getUnitDoubleValue(stringIDToTypeID('size')),
			color: c.rgb.hexValue
		};
	} else {
		return {
			localLightingAngle: desc.getUnitDoubleValue(stringIDToTypeID('localLightingAngle')),
			distance:           desc.getUnitDoubleValue(stringIDToTypeID('distance')),
			opacity:            desc.getUnitDoubleValue(stringIDToTypeID('opacity')),
			color:              c.rgb.hexValue
		};
	}
}

// 텍스트 x 스케일 취득
function get_text_x_scale(text_item) {
	app.activeDocument.activeLayer = text_item.parent;
	var ref = new ActionReference();
	ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
	var desc = executeActionGet(ref).getObjectValue(stringIDToTypeID('textKey'));

	if (desc.hasKey(stringIDToTypeID('transform'))) {
		return desc.getObjectValue(stringIDToTypeID('transform'))
		           .getUnitDoubleValue(stringIDToTypeID('xx'));
	}
	return 1;
}

// 한글 → 두벌식 로마자 변환
function convert_kor_to_eng(word) {
	var MAX_LEN = 15;
	if (word.length > MAX_LEN) word = word.substring(0, MAX_LEN - 1);

	var result = "";
	for (var i = 0; i < word.length; i++) {
		var code = word.charCodeAt(i) - 0xAC00;

		if (code >= 0 && code <= 11172) {
			var cho  = parseInt(code / (21 * 28));
			var jung = parseInt(code % (21 * 28) / 28);
			var jong = parseInt(code % 28);

			result += arr_cho_sung_eng[cho] + arr_jung_sung_eng[jung];
			if (jong != 0) result += arr_jong_sung_eng[jong];
		} else if (code >= 34097 && code <= 34126) {
			result += arr_single_jaum_eng[code - 34097];
		} else if (code >= 34127 && code <= 34147) {
			result += arr_jung_sung_eng[code - 34127];
		} else {
			result += word.charAt(i);
		}
	}
	return result;
}

// 설정 팝업
function show_settings_dialog() {
	if (parseInt(app.version) < 22) {
		alert("Photoshop 2021 이상 버전을 사용해주세요.");
		return;
	}
	if (!original_doc) {
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
		dialog = new Window("dialog", "PhotoshopToUnity v" + script_version);
	} catch (e) {
		throw new Error("\n\n알 수 없는 원인으로 스크립트를 실행할 수 없습니다. 포토샵을 다시 켜주세요.\n\n" + e.message);
	}
	dialog.alignChildren = "fill";

	var settings_group = dialog.add("panel", undefined, "유효한 접두사 리스트");
	settings_group.margins = [10, 15, 10, 10];
	settings_group.alignChildren = "fill";
	var help_text = settings_group.add("statictext", undefined,
		  "• btn_  → 버튼\n"
		+ "• icon_ → 아이콘(이미지)\n"
		+ "• deco_ → 데코 이미지\n"
		+ "• img_  → UI 이미지\n"
		+ "• bg_   → BG / 타이틀 이미지\n"
		+ "• inner_→ 팝업 이너\n"
		+ "• item_ → 아이템 이미지\n"
		+ "• txt_  → TextMeshPro 텍스트 (자동 생성)",
		{multiline: true});
	help_text.preferredSize.width = 325;

	var button_group = dialog.add("group");
	var group = button_group.add("group");
	group.alignment = ["fill", ""];
	group.alignChildren = ["center", ""];
	var run_button    = group.add("button", undefined, "OK");
	var cancel_button = group.add("button", undefined, "Cancel");

	cancel_button.onClick = function () { dialog.close(); };

	run_button.onClick = function () {
		run_button.enabled    = false;
		cancel_button.enabled = false;

		var ruler_units = app.preferences.rulerUnits;
		app.preferences.rulerUnits = Units.PIXELS;
		try {
            // 원본 보호를 위해 작업용 파일 복제
            var doc_name = original_doc.name.replace(/\.[^\.]+$/, '');
            var work_doc = original_doc.duplicate(doc_name + "_exported");
            app.activeDocument = work_doc;

            // 1. 요청하신 [밑에서부터 -> 래스터화 -> 마스크 병합] 로직 실행
            flatten_effects_and_masks(app.activeDocument);

            // 2. 기존의 텍스트 파싱 및 레이어 이름 변경 로직 실행
			rename_layers(app.activeDocument);
            
            alert("작업이 완료되었습니다.\n원본 파일 보호를 위해 복제된 '_exported' 파일에서 병합이 진행되었습니다.");

		} catch (e) {
			if (e.message == "User cancelled the operation") return;
			alert("An unexpected error has occurred:\n\n[line " + e.line + "] " + e.message
				+ "\n\nTo debug, run the script using Adobe ExtendScript"
				+ " with \"Debug > Do not break on guarded exceptions\" unchecked.");
			debugger;
		} finally {
			app.preferences.rulerUnits = ruler_units;
			dialog.close();
		}
	};

	dialog.center();
	dialog.show();
}