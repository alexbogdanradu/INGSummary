$('#btn_upload').on('click', function (event) {
    event.preventDefault();
    if ($('#file_field').get(0).files.length === 0) {
        return;
    }
    else {
        $.ajax({
            // Your server script to process the upload
            url: '/',
            type: 'POST',

            // Form data
            data: new FormData($('form')[0]),

            // Tell jQuery not to process data or worry about content-type
            // You *must* include these options!
            cache: false,
            contentType: false,
            processData: false,

            success: function (data) {
                //console.log(data);
                $('#uploadSection').hide();
                $('#results').show();
                SetFields(data);
            },
            error: function (jqXHR, exception) {
            		console.log(jqXHR);
            		console.log(exception);
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
                    $('#alertError').show();
                    $('#alertError').append(msg);

                    console.log(msg);
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
                            $('#progressBar').css('width', prog+'%').attr('aria-valuenow', prog);
                            //$('.progress-bar').css('width', valeur+'%').attr('aria-valuenow', valeur);      
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

    transactions.forEach(function (week) {
        console.log(week);
        $("#tableBody").append('<tr><th scope="row">' + week.WeekNo + '</th><td>' + week.TotalSpent + ' RON</td><td>' + week.TotalSpentPreviousPercent + ' %</td></tr>');
    });
}