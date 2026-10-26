(function () {
  var sections = [
    { title: "Getting Started", pages: [
      ["index.html", "Introduction & Quick Start"],
      ["first-level.html", "Creating Your First Level"],
      ["architecture.html", "Core Architecture"]
    ]},
    { title: "Content", pages: [
      ["objects.html", "Objects, Categories & Rules"],
      ["placement.html", "Placement, Containers & Stacking"],
      ["objectives.html", "Objectives & Level Progression"]
    ]},
    { title: "Gameplay Systems", pages: [
      ["gameplay-systems.html", "Scoring, Combo, Hints, Cleaning & Before/After"],
      ["input.html", "Input: Keyboard, Gamepad & Touch"],
      ["player.html", "Player Controller & Interaction"],
      ["ui-audio-save.html", "UI, Audio, Save & Settings"]
    ]},
    { title: "Production", pages: [
      ["performance.html", "Performance, Mobile & URP"],
      ["extending.html", "Customising & Extending"],
      ["demos.html", "Demo Scenes"],
      ["api.html", "API Reference"],
      ["troubleshooting.html", "Troubleshooting & FAQ"],
      ["whats-new.html", "What's New & Changelog"]
    ]}
  ];

  var current = location.pathname.split("/").pop() || "index.html";
  var nav = document.querySelector("nav.sidebar");
  if (!nav) { return; }

  var html = '<div class="brand">Tidy Up Framework</div>' +
    '<div class="tagline">Tidy Up + Sorting + Organizing + Placement + Satisfying Interaction</div>';
  var flat = [];
  sections.forEach(function (section) {
    html += "<h4>" + section.title + "</h4>";
    section.pages.forEach(function (page) {
      flat.push(page);
      var active = page[0] === current ? ' class="active"' : "";
      html += '<a href="' + page[0] + '"' + active + ">" + page[1] + "</a>";
    });
  });
  nav.innerHTML = html;

  var main = document.querySelector("main");
  var index = flat.findIndex(function (page) { return page[0] === current; });
  if (main && index >= 0) {
    var pager = document.createElement("div");
    pager.className = "pager";
    var previous = index > 0 ? '<a href="' + flat[index - 1][0] + '">&larr; ' + flat[index - 1][1] + "</a>" : "<span></span>";
    var next = index < flat.length - 1 ? '<a href="' + flat[index + 1][0] + '">' + flat[index + 1][1] + " &rarr;</a>" : "<span></span>";
    pager.innerHTML = previous + next;
    main.appendChild(pager);
  }
})();
