$('#btn_upload').on('click', function (event) {
    event.preventDefault();
    if ($('#file_field').get(0).files.length === 0) {
        return;
    }
    else {
        $.ajax({
            url: '/',
            type: 'POST',
            data: new FormData($('form')[0]),
            cache: false,
            contentType: false,
            processData: false,

            success: function (data) {
                console.log(data);
                $('#uploadSection').hide();
                $('#results').show();
                SetFields(data);
            },
            error: function (jqXHR, exception) {
                var msg = '';
                if (jqXHR.status === 0) {
                    msg = 'Not connect.\n Verify Network.';
                } else if (jqXHR.status == 404) {
                    msg = 'Requested page not found. [404]';
                } else if (jqXHR.status == 500) {
                    msg = 'Internal Server Error [500].';
                } else if (exception === 'parsererror') {
                    msg = 'Requested JSON parse failed.';
                } else if (exception === 'timeout') {
                    msg = 'Time out error.';
                } else if (exception === 'abort') {
                    msg = 'Ajax request aborted.';
                } else {
                    msg = 'Uncaught Error.\n' + jqXHR.responseText;
                }
                console.log(msg);
                $('#alertError').show();
                $('#alertError').append(msg);
            },
            // Custom XMLHttpRequest
            xhr: function () {
                var myXhr = $.ajaxSettings.xhr();
                if (myXhr.upload) {
                    // For handling the progress of the upload
                    myXhr.upload.addEventListener('progress', function (e) {
                        if (e.lengthComputable) {
                            var prog = Math.floor(e.loaded / e.total * 100);
                            console.log(prog);
                            $('#progressBar').css('width', prog + '%').attr('aria-valuenow', prog);
                        }
                    }, false);
                }
                return myXhr;
            }
        });
    }
});

function SetFields(data) {
    var transactions = JSON.parse(data);
    console.log('been before the for');
    transactions.forEach(function (week) {
        console.log(week);
        $('#summary').append('<button type="button" class="list-group-item list-group-item-action">' + 'Week: '+ week.WeekNo + '</button>');
        week.Payments.forEach(function (payment) {
            console.log(payment);
            //$('#details1').append('<button type="button" class="list-group-item list-group-item-action">' + week.TotalSpent + ' RON' + '</button>');
            //$('#details2').append('<button type="button" class="list-group-item list-group-item-action">' + week.TotalSpent + ' RON' + '</button>');
        });
    });
}