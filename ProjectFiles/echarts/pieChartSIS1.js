// Espera a que el DOM esté completamente cargado antes de inicializar el gráfico
document.addEventListener('DOMContentLoaded', function () {
    var dom = document.getElementById('container');
    var myChart = echarts.init(dom);

    var option = {
        tooltip: {
            trigger: 'item'
        },
        legend: {
            orient: 'vertical',
            left: 'left',
            itemGap: 1,
            top: '10%'
            
            
            
        },
        color: ['#1f263e', '#052377', '#04bfda', '#3fa5c3', '#274e8e'],
        series: [
            {
                
                type: 'pie',
                radius: '75%',
                center: ['65%', '50%'], 
                data: [
                    { value: $01, name: '1oo1' },
                    { value: $02, name: '1oo2' },
                    { value: $03, name: '2oo2' },
                    { value: $04, name: '2oo3' },
                    { value: $04, name: '2oo4' },
                    
                   
                ],
                emphasis: {
                    itemStyle: {
                        shadowBlur: 10,
                        shadowOffsetX: 0,
                        shadowColor: 'rgba(0, 0, 0, 0.5)'
                    }
                },
                labelLine: {
                    show: false // Desactiva las líneas de las etiquetas
                },
                label: {
                    show: false // Oculta las etiquetas
                },
            }
        ]
    };

    // Establecer la opción del gráfico
    myChart.setOption(option);
    
    // Ajustar el tamaño del gráfico cuando se redimensiona la ventana
    window.addEventListener('resize', myChart.resize);
});
