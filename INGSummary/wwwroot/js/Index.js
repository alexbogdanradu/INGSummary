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

				success: function(data){
                    //console.log(data);
                    $('#uploadSection').hide();
                    $('#results').show();
                    SetFields(data);
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
                                //$('#progressBar').text(Math.floor(e.loaded / e.total * 100) + '%');
                                //$('#progressBar').css('width', (e.loaded / e.total * 100) +'%').attr('aria-valuenow', (e.loaded / e.total * 100)); 
                                //$(".progress-bar").attr('aria-valuenow', prog);   
                                $('#progressBar').css('width', prog +'%').attr('aria-valuenow', prog);
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

    transactions.forEach(function (weeks) {
        weeks.forEach(function (week) {
            console.log(week);
        });
        
    });

    $("#resultsTitle").append(transactions);
}