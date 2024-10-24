// Espera a que el DOM esté completamente cargado antes de inicializar el gráfico
document.addEventListener('DOMContentLoaded', function () {
    var dom = document.getElementById('container');
    var myChart = echarts.init(dom);

    var option = {
        tooltip: {
            trigger: 'item'
        },
        legend: {
            orient: 'horizontal',
            left: 'center',
            itemGap: 20,
            top: '88%',
            
        },
        color: ['#1f263e', '#052377', '#04bfda', '#80edfd', '#5a8fd0'],
        series: [
            {
                
                type: 'pie',
                radius: '75%',
                data: [
                    { value: $01, name: 'Manual' },
                    { value: $02, name: 'Auto' },
                    
                   
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
