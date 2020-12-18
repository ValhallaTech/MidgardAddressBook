$('a[data-toggle="list"]').on("shown.bs.tab", function(e) {
  e.target.style.backgroundColor = "rebeccapurple";
  e.target.style.borderColor = "rebeccapurple";
  e.relatedTarget.style.backgroundColor = "white";
  e.relatedTarget.style.borderColor = "lightgrey";
});
