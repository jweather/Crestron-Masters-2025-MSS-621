var sources = [
	{ name: 'Global Source 1', ip: '1.1.1.1' },
	{ name: 'Global Source 2', ip: '2.2.2.2' }
];

$(window).load(function () {
	$.ajaxSetup({
		cache: false,
		contentType: 'application/json',
		error: function (xhr, textStatus, error) {
			alert("Server error: " + xhr.responseText);
		}
	});

	// on startup
	$.get('../cws/nvx/data', function (data) {
		$('#startupLoader').hide();
		refresh(data);
	});
}); // window.load

function refresh(data) {
	$('tr.dynamic').remove();
	data.forEach(row => {
		$('#sources .btnRow').before($('<tr class="dynamic" data-name="' + row.name + '"><td>' + row.name + '</td><td>' + row.ip + '</td>' +
			'<td><button type="button" class="btn btn-danger btnClear">Delete</button></td>'));
	});
}


$(document).on('click', '#btnAdd', function (e) {
	var entry = { name: $('#addName').val(), 'ip': $('#addIP').val() };
	if (!entry.name || !entry.ip) {
		return alert("Please enter a name and IP first.");
	}
	$.post('../cws/nvx/add', JSON.stringify(entry), function (data) {
		$('#addName').val(''); $('#addIP').val('');
		refresh(data);
	});
});

$(document).on('click', '.btnClear', function (e) {
	var name = $(this).parents('tr').data('name');
	$.post('../cws/nvx/delete/' + encodeURIComponent(name), function (data) {
		console.log('cleared');
		refresh(data);
	}).fail(function (err) {
		alert(err);
	});
});
