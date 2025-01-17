# Lisa Alert Drone Dataset (LADD)

LADD is a dataset of drone created images for pedestrian detection. LADD annotations are into VOC format. 

You can download [version 1](https://yadi.sk/d/4Hz_1qpiNbHhpQ) or [version 2](https://yadi.sk/d/IBoACy3LOG8Cig) of the LADD from Yandex.Disk directly. 

#### Overview of dataset

* You can see a example of the labeled  image.

  We have just one kind of label :

  * 0 - Pedestrian

  ![example](../docs/imgs/examples/01.png)


* The structure of the `LADD_v1`

  ```
  ├── LADD
  │   ├── Annotations
  │   │   └── X.xml (419 items)
  │   ├── examples
  │   │   └── X.jpg (10 items)
  │   ├── ImageSets
  │   │   └── Main 
            # *.txt which split the dataset
  │   │       └──  test.txt
  │   │       └──  train.txt
  │   │       └──  trainval.txt
  │   │       └──  val.txt
  │   └── JPEGImages
  │       └── X.jpg (419 items)

  ```

* The  `JPEGImages`:

  * **Image Type** : *jpeg(JPEG)*
  * **Width** x **Height** : *4000 x 3000*

* The `Annotations` : The VOC format `.xml` for Object Detection, automatically generate by the label tools. Below is an example of `.xml` file.

```xml
<annotation>
    <folder>VocGalsTfl</folder>
    <filename>0</filename>
    <source>
        <database>Unknown</database>
    </source>
    <size>
        <width>4000</width>
        <height>3000</height>
        <depth>3</depth>
    </size>
    <segmented>0</segmented>
    <object>
        <name>Pedestrian</name>
        <pose>Unspecified</pose>
        <truncated>0</truncated>
        <difficult>0</difficult>
        <bndbox>
            <xmin>1881</xmin>
            <ymin>1409</ymin>
            <xmax>1905</xmax>
            <ymax>1469</ymax>
        </bndbox>
    </object>
    <object>
        ...
    </object>
</annotation> 
```



