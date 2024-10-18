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
            itemGap: 1,
            top: '88%',
            
        },
        color: ['#0f2d57', '#1c3e7a', '#29509d', '#3a6ab3', '#5a8fd0'],
        series: [
            {
                
                type: 'pie',
                radius: '75%',
                data: [
                    { value: $01, name: '1' },
                    { value: $02, name: '2' },
                    { value: $03, name: '3' },
                    { value: $04, name: '4' },
                   
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
