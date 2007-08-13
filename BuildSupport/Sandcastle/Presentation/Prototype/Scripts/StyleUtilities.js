
	function getStyleDictionary() {
		var dictionary = new Array();

		// iterate through stylesheets
		var sheets = document.styleSheets;
		for(var i=0; i<sheets.length;i++) {
			var sheet = sheets[i];

            // ignore sheets at ms-help Urls
            if (sheet.href.substr(0,8) == 'ms-help:') continue;

			// get sheet rules
			var rules = sheet.cssRules;
			if (rules == null) rules = sheet.rules;

			// iterate through rules
			for(j=0; j<rules.length; j++) {
				var rule = rules[j];

				// add rule to dictionary
				dictionary[rule.selectorText] = rule.style;

			}
		}

		return(dictionary);

	}

	function toggleVisibleLanguage(id) {

		if (id == 'cs') {
			sd['SPAN.cs'].display = 'inline';
			sd['SPAN.vb'].display = 'none';
		} else {
			sd['SPAN.cs'].display = 'none';
			sd['SPAN.vb'].display = 'inline';
		}

	}

