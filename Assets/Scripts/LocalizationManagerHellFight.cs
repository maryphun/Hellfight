using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.SimpleLocalization;

public class LocalizationManagerHellFight : Singleton<LocalizationManagerHellFight>
{
    public void Initialization(SystemLanguage language = SystemLanguage.English)
    {
		LocalizationManager.Read();

		switch (language)
		{
			case SystemLanguage.ChineseSimplified:
				LocalizationManager.Language = "ChineseSimplified";
				break;
			case SystemLanguage.ChineseTraditional:
				LocalizationManager.Language = "ChineseTraditional";
				break;
			case SystemLanguage.Japanese:
				LocalizationManager.Language = "Japanese";
				break;
			default:
				LocalizationManager.Language = "English";
				break;
		}
	}

	public string GetCurrentLanguage()
    {
		return LocalizationManager.Language;
	}

	public void SetCurrentLanguage(string language)
	{
		//GameObject.Find("LANGUAGEDEBUG").GetComponent<TMPro.TMP_Text>().SetText(language);
		LocalizationManager.Language = language;
	}
}
